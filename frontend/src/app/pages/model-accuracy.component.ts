import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subscription, forkJoin, timer } from 'rxjs';
import { finalize, switchMap, takeWhile } from 'rxjs/operators';
import { StockApiService } from '../core/stock-api.service';
import { ModelTrainingJob, ModelVersion, TickerModelMetrics } from '../core/models';
import { normalizeTicker, tickerValidationMessage } from '../core/ticker-validation';
import { InfoTipComponent } from '../shared/info-tip.component';

@Component({
  imports: [CommonModule, FormsModule, InfoTipComponent],
  template: `
    <main class="page">
      <p class="eyebrow">MODEL ACCURACY <app-info-tip text="Share of historical test predictions the model got right." /></p>
      <h1>Model performance by ticker</h1>
      <p class="lead">Review chronological holdout results, retrain without blocking the page, and inspect every saved model version.</p>

      <form class="accuracy-search card" (ngSubmit)="loadMetrics()" novalidate>
        <label for="accuracy-ticker">Ticker symbol</label>
        <div class="search-row">
          <input id="accuracy-ticker" name="ticker" [(ngModel)]="ticker" (ngModelChange)="validationError.set('')" maxlength="20" autocomplete="off" placeholder="e.g. RELIANCE.NS or AAPL" />
          <button type="submit" [disabled]="loading() || training()">{{ loading() ? 'Loading...' : 'View metrics' }}</button>
        </div>
        @if (validationError()) { <p class="field-error" role="alert">{{ validationError() }}</p> }
      </form>

      @if (loading()) { <div class="loading card" role="status"><span class="spinner" aria-hidden="true"></span> Loading model metrics...</div> }
      @if (error()) { <div class="empty card" role="alert"><h2>{{ error() }}</h2><p>Try another ticker or open Stock Detail to create its model.</p></div> }
      @if (message()) { <div class="notice success-notice" role="status">{{ message() }}</div> }

      @if (job(); as currentJob) {
        <section class="training-job card" aria-live="polite">
          <div><span class="badge model-badge">{{ jobLabel(currentJob) }}</span><h2>Background retraining</h2><p>{{ jobDescription(currentJob) }}</p></div>
          @if (currentJob.status === 'queued' || currentJob.status === 'running') { <span class="spinner" aria-hidden="true"></span> }
        </section>
      }

      @if (metrics(); as model) {
        <div class="model-heading">
          <div><span class="badge model-badge">{{ model.model_status === 'trained' ? 'Trained Model' : model.model_status }}</span><h2>{{ model.ticker }}</h2><p class="muted">{{ model.model_name }}</p></div>
          <div class="model-actions"><label>Training history<select name="trainingPeriod" [(ngModel)]="trainingPeriod" [disabled]="training()"><option value="1y">1 year</option><option value="2y">2 years</option><option value="5y">5 years</option><option value="max">Full history</option></select></label><button class="secondary-button" type="button" (click)="startRetraining()" [disabled]="training()">{{ training() ? 'Training in background...' : 'Retrain in background' }}</button><div class="trained-date"><span>Active model trained</span><strong>{{ model.trained_at | date:'medium' }}</strong></div></div>
        </div>
        <section class="metric-grid accuracy-metrics">
          <article class="card"><span>Model accuracy <app-info-tip text="Share of historical test predictions the model got right." /></span><strong>{{ model.accuracy | percent:'1.0-1' }}</strong><p>Overall correct predictions</p></article>
          <article class="card"><span>Precision</span><strong>{{ model.precision | percent:'1.0-1' }}</strong><p>Quality of predicted UP signals</p></article>
          <article class="card"><span>Recall</span><strong>{{ model.recall | percent:'1.0-1' }}</strong><p>Share of actual UP days found</p></article>
        </section>
        <section class="content-grid">
          <article class="card"><h2>Confusion matrix</h2><div class="matrix"><div><span>True DOWN</span><strong>{{ matrixValue(model, 0, 0) }}</strong></div><div><span>False UP</span><strong>{{ matrixValue(model, 0, 1) }}</strong></div><div><span>False DOWN</span><strong>{{ matrixValue(model, 1, 0) }}</strong></div><div><span>True UP</span><strong>{{ matrixValue(model, 1, 1) }}</strong></div></div></article>
          <article class="card"><h2>Dataset split</h2><div class="row-counts"><div><span>Training rows</span><strong>{{ model.training_rows | number }}</strong></div><div><span>Testing rows</span><strong>{{ model.testing_rows | number }}</strong></div></div><div class="notice">Training: first 80% · Testing: final 20% · No shuffle</div><p class="body-copy">Metrics describe a historical test period and do not guarantee future performance.</p></article>
        </section>
      }

      @if (versions().length) {
        <section class="version-section">
          <div class="section-heading"><div><p class="eyebrow">MODEL REGISTRY</p><h2>Version history</h2></div><span class="muted">{{ versions().length }} saved version{{ versions().length === 1 ? '' : 's' }}</span></div>
          <div class="version-list">
            @for (version of versions(); track version.version_id) {
              <article class="card version-card">
                <div class="version-title"><div><span class="badge" [class.model-badge]="version.is_active">{{ version.is_active ? 'Active' : 'Archived' }}</span><h3>{{ version.trained_at | date:'medium' }}</h3></div><code>{{ shortVersion(version.version_id) }}</code></div>
                <div class="version-metrics"><span>Model accuracy <app-info-tip text="Share of historical test predictions the model got right." /><strong>{{ version.accuracy | percent:'1.0-1' }}</strong></span><span>Precision <strong>{{ version.precision | percent:'1.0-1' }}</strong></span><span>Recall <strong>{{ version.recall | percent:'1.0-1' }}</strong></span><span>Rows <strong>{{ version.training_rows | number }} / {{ version.test_rows | number }}</strong></span></div>
              </article>
            }
          </div>
        </section>
      }
    </main>`,
})
export class ModelAccuracyComponent implements OnInit, OnDestroy {
  private readonly api = inject(StockApiService);
  ticker = 'RELIANCE.NS';
  trainingPeriod = '5y';
  readonly loading = signal(false);
  readonly error = signal('');
  readonly validationError = signal('');
  readonly metrics = signal<TickerModelMetrics | null>(null);
  readonly versions = signal<ModelVersion[]>([]);
  readonly job = signal<ModelTrainingJob | null>(null);
  readonly training = signal(false);
  readonly message = signal('');
  private polling?: Subscription;

  ngOnInit(): void { this.loadMetrics(); }

  loadMetrics(): void {
    this.validationError.set(tickerValidationMessage(this.ticker));
    if (this.validationError()) return;
    this.ticker = normalizeTicker(this.ticker);
    this.loading.set(true); this.error.set(''); this.message.set(''); this.metrics.set(null); this.versions.set([]);
    forkJoin({ metrics: this.api.getModelMetrics(this.ticker), versions: this.api.getModelVersions(this.ticker) }).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: ({ metrics, versions }) => { this.metrics.set(metrics); this.versions.set(versions.versions); },
      error: (error) => this.error.set(error.status === 404 ? 'No trained model found for this ticker. Analyze or train this stock first.' : error.error?.detail ?? 'Model metrics could not be loaded.'),
    });
  }

  startRetraining(): void {
    this.validationError.set(tickerValidationMessage(this.ticker));
    if (this.validationError()) return;
    this.ticker = normalizeTicker(this.ticker);
    this.training.set(true); this.error.set(''); this.message.set('');
    this.api.startTrainingJob(this.ticker, this.trainingPeriod).subscribe({
      next: (job) => { this.job.set(job); this.pollJob(job.job_id); },
      error: (error) => { this.training.set(false); this.error.set(error.error?.detail ?? 'Training could not be started.'); },
    });
  }

  ngOnDestroy(): void { this.polling?.unsubscribe(); }

  jobLabel(job: ModelTrainingJob): string {
    return job.status === 'succeeded' ? 'Training complete' : job.status === 'failed' ? 'Training failed' : job.status === 'running' ? 'Training' : 'Queued';
  }

  jobDescription(job: ModelTrainingJob): string {
    if (job.status === 'failed') return job.error ?? 'The training pipeline failed.';
    if (job.status === 'succeeded') return `The new ${job.ticker} model is active with ${((job.accuracy ?? 0) * 100).toFixed(1)}% holdout accuracy.`;
    if (job.status === 'running') return `Fetching history, calculating indicators, and training ${job.ticker}. You can leave this page while it runs.`;
    return `${job.ticker} is waiting for an available training worker.`;
  }

  shortVersion(versionId: string): string { return versionId.slice(0, 15); }

  matrixValue(model: TickerModelMetrics, row: number, column: number): number {
    return model.confusion_matrix[row]?.[column] ?? 0;
  }

  private pollJob(jobId: string): void {
    this.polling?.unsubscribe();
    this.polling = timer(0, 1500).pipe(
      switchMap(() => this.api.getTrainingJob(jobId)),
      takeWhile((job) => job.status === 'queued' || job.status === 'running', true),
    ).subscribe({
      next: (job) => {
        this.job.set(job);
        if (job.status === 'succeeded') {
          this.training.set(false);
          this.loadMetrics();
          this.message.set(`${job.ticker} trained successfully. Metrics and version history are refreshed.`);
        } else if (job.status === 'failed') {
          this.training.set(false);
          this.error.set(job.error ?? 'Model training failed.');
        }
      },
      error: (error) => { this.training.set(false); this.error.set(error.error?.detail ?? 'Training status could not be loaded.'); },
    });
  }
}

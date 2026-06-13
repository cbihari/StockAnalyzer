import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LEARNING_GLOSSARY, LEARNING_LESSONS, LearningLesson, findLesson } from '../core/learning-content';
import { LearningProgressService } from '../core/learning-progress.service';
import { InfoTipComponent } from '../shared/info-tip.component';

@Component({
  imports: [CommonModule, RouterLink, InfoTipComponent],
  template: `
    <main class="page learning-page">
      @if (lesson(); as item) {
        <a class="back-link" routerLink="/learn">← Learning Center</a>
        <div class="lesson-heading"><div><p class="eyebrow">{{ item.category | uppercase }} · {{ item.duration }}</p><h1>{{ item.title }}</h1><p class="lead">{{ item.summary }}</p></div><div class="lesson-progress"><span>{{ progress.has(item.slug) ? 'COMPLETED' : 'LESSON' }}</span><strong>{{ completedCount() }}/{{ lessons.length }}</strong><small>foundations complete</small></div></div>
        <section class="lesson-layout">
          <article class="lesson-body">
            <section><span class="lesson-number">01</span><h2>What it means</h2><p>{{ item.definition }}</p></section>
            <section><span class="lesson-number">02</span><h2>Why it matters</h2><p>{{ item.matters }}</p></section>
            <section><span class="lesson-number">03</span><h2>How to read it</h2><div class="interpretation-grid">@for (point of item.interpretation; track point.label) { <div><strong>{{ point.label }}</strong><p>{{ point.detail }}</p></div> }</div></section>
            <aside class="mistake-callout"><span>COMMON MISTAKE</span><strong>{{ item.mistake }}</strong></aside>
            <section><span class="lesson-number">04</span><h2>Practical example</h2><p>{{ item.example }}</p></section>
            <aside class="takeaway-callout"><span>KEY TAKEAWAY</span><strong>{{ item.takeaway }}</strong></aside>
            <section class="quiz-card card">
              <p class="eyebrow">KNOWLEDGE CHECK</p><h2>{{ item.quiz.question }}</h2>
              <div class="quiz-options">@for (option of item.quiz.options; track option; let index = $index) { <button type="button" [class.selected]="selectedAnswer() === index" [class.correct]="quizRevealed() && index === item.quiz.answer" [class.incorrect]="quizRevealed() && selectedAnswer() === index && index !== item.quiz.answer" (click)="selectAnswer(index)">{{ option }}</button> }</div>
              @if (quizRevealed()) { <p class="quiz-result" role="status"><strong>{{ selectedAnswer() === item.quiz.answer ? 'Correct.' : 'Not quite.' }}</strong> {{ item.quiz.explanation }}</p> }
            </section>
          </article>
          <aside class="lesson-sidebar card"><span>YOUR PROGRESS</span><strong>{{ progress.has(item.slug) ? 'Lesson understood' : 'Ready when you are' }}</strong><p>Completion is saved in this browser. It is a learning marker, not a certification.</p><button type="button" [disabled]="progress.has(item.slug)" (click)="complete(item.slug)">{{ progress.has(item.slug) ? 'Completed' : 'Mark as understood' }}</button><a routerLink="/stocks/AAPL">See indicators on AAPL →</a></aside>
        </section>
      } @else {
        <div class="learning-heading"><div><p class="eyebrow">LEARNING CENTER</p><h1>Understand unfamiliar research terms.</h1><p class="lead">Use these short lessons when a dashboard indicator or model output needs context.</p></div><div class="learning-score"><strong>{{ completedCount() }}/{{ lessons.length }}</strong><span>lessons understood</span><div><i [style.width.%]="completionPercent()"></i></div></div></div>
        <section class="learning-paths">
          @for (lesson of lessons; track lesson.slug; let index = $index) {
            <a class="lesson-card card" [routerLink]="['/learn', lesson.slug]"><div class="lesson-card-top"><span>{{ (index + 1).toString().padStart(2, '0') }}</span><b [class.complete]="progress.has(lesson.slug)">{{ progress.has(lesson.slug) ? 'UNDERSTOOD' : lesson.duration }}</b></div><p>{{ lesson.category }}</p><h2>{{ lesson.title }}</h2><small>{{ lesson.summary }}</small><strong>Start lesson →</strong></a>
          }
        </section>
        <section class="glossary-section"><div><p class="eyebrow">QUICK REFERENCE</p><h2>Research glossary</h2></div><div class="glossary-grid">@for (term of glossary; track term[0]) { <article><strong>{{ term[0] }} @if (tipFor(term[0]); as tip) { <app-info-tip [text]="tip" /> }</strong><p>{{ term[1] }}</p></article> }</div></section>
        <div class="learning-disclaimer notice">Education explains market concepts but cannot remove uncertainty or determine whether an investment is suitable for you.</div>
      }
    </main>`,
})
export class LearningCenterComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  readonly progress = inject(LearningProgressService);
  readonly lessons = LEARNING_LESSONS;
  readonly glossary = LEARNING_GLOSSARY;
  readonly lesson = signal<LearningLesson | undefined>(undefined);
  readonly selectedAnswer = signal<number | null>(null);
  readonly quizRevealed = signal(false);

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      this.lesson.set(findLesson(params.get('slug')));
      this.selectedAnswer.set(null);
      this.quizRevealed.set(false);
    });
  }

  completedCount(): number { return this.progress.completed().filter((slug) => this.lessons.some((lesson) => lesson.slug === slug)).length; }
  completionPercent(): number { return this.completedCount() / this.lessons.length * 100; }
  selectAnswer(index: number): void { this.selectedAnswer.set(index); this.quizRevealed.set(true); }
  complete(slug: string): void { this.progress.markComplete(slug); }
  tipFor(term: string): string {
    const tips: Record<string, string> = {
      RSI: 'Shows whether recent price moves may be overbought or oversold.',
      MACD: 'Shows if price momentum is speeding up or slowing down.',
      EMA: 'Tracks price trends with extra weight on newer prices.',
      SMA: 'Shows the average closing price across a fixed period.',
      Volatility: 'How widely and quickly the stock price tends to move.',
      Support: 'A recent price area where declines have often slowed.',
      Resistance: 'A recent price area where advances have often slowed.',
      Confidence: 'How strongly the model favors its estimate based on current inputs.',
    };
    return tips[term] ?? '';
  }
}

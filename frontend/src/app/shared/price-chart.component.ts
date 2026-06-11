import { AfterViewInit, Component, ElementRef, Input, OnChanges, OnDestroy, SimpleChanges, ViewChild } from '@angular/core';
import { Chart, ChartConfiguration, registerables } from 'chart.js';
import { HistoricalPrice } from '../core/models';

Chart.register(...registerables);

@Component({
  selector: 'app-price-chart',
  template: '<canvas #canvas></canvas>',
  styles: ':host { display: block; height: 330px; } canvas { width: 100% !important; height: 100% !important; }',
})
export class PriceChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  @Input({ required: true }) prices: HistoricalPrice[] = [];
  @ViewChild('canvas') canvas?: ElementRef<HTMLCanvasElement>;
  private chart?: Chart;

  ngAfterViewInit(): void { this.render(); }
  ngOnChanges(changes: SimpleChanges): void { if (changes['prices'] && this.canvas) this.render(); }
  ngOnDestroy(): void { this.chart?.destroy(); }

  private render(): void {
    if (!this.canvas || !this.prices.length) return;
    this.chart?.destroy();
    const config: ChartConfiguration<'line'> = {
      type: 'line',
      data: {
        labels: this.prices.map((price) => price.date),
        datasets: [{
          label: 'Closing price',
          data: this.prices.map((price) => price.close),
          borderColor: '#65d49b',
          backgroundColor: 'rgba(101, 212, 155, .08)',
          fill: true,
          pointRadius: 0,
          borderWidth: 2,
          tension: .18,
        }],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: { intersect: false, mode: 'index' },
        plugins: { legend: { display: false } },
        scales: {
          x: { display: false, grid: { display: false } },
          y: { ticks: { color: '#839087' }, grid: { color: 'rgba(255,255,255,.05)' } },
        },
      },
    };
    this.chart = new Chart(this.canvas.nativeElement, config);
  }
}

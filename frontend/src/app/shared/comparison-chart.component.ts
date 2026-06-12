import { AfterViewInit, Component, ElementRef, Input, OnChanges, OnDestroy, SimpleChanges, ViewChild } from '@angular/core';
import { Chart, ChartConfiguration, registerables } from 'chart.js';
import { StockAnalysis } from '../core/models';

Chart.register(...registerables);

@Component({
  selector: 'app-comparison-chart',
  template: '<canvas #canvas></canvas>',
  styles: ':host { display: block; height: 390px; } canvas { width: 100% !important; height: 100% !important; }',
})
export class ComparisonChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  @Input({ required: true }) stocks: StockAnalysis[] = [];
  @ViewChild('canvas') canvas?: ElementRef<HTMLCanvasElement>;
  private chart?: Chart;

  ngAfterViewInit(): void { this.render(); }
  ngOnChanges(changes: SimpleChanges): void { if (changes['stocks'] && this.canvas) this.render(); }
  ngOnDestroy(): void { this.chart?.destroy(); }

  private render(): void {
    if (!this.canvas || this.stocks.length < 2) return;
    this.chart?.destroy();
    const dates = [...new Set(this.stocks.flatMap((stock) => stock.history.map((price) => price.date)))].sort();
    const colors = ['#58eba6', '#74a7ff', '#f4bd64'];
    const config: ChartConfiguration<'line'> = {
      type: 'line',
      data: {
        labels: dates,
        datasets: this.stocks.map((stock, index) => {
          const firstClose = stock.history[0]?.close ?? 1;
          const values = new Map(stock.history.map((price) => [price.date, (price.close / firstClose) * 100]));
          return {
            label: stock.ticker,
            data: dates.map((date) => values.get(date) ?? null),
            borderColor: colors[index],
            backgroundColor: colors[index],
            pointRadius: 0,
            borderWidth: 2,
            spanGaps: true,
            tension: .16,
          };
        }),
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: { intersect: false, mode: 'index' },
        plugins: { legend: { labels: { color: '#c7d0ca', usePointStyle: true, pointStyle: 'line' } } },
        scales: {
          x: { display: false, grid: { display: false } },
          y: { title: { display: true, text: 'Indexed performance (start = 100)', color: '#87938b' }, ticks: { color: '#87938b' }, grid: { color: 'rgba(255,255,255,.05)' } },
        },
      },
    };
    this.chart = new Chart(this.canvas.nativeElement, config);
  }
}

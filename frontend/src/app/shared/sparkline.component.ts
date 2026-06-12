import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-sparkline',
  template: `<svg viewBox="0 0 120 34" role="img" aria-label="Recent closing price trend"><polyline [attr.points]="points()" fill="none" [attr.stroke]="negative ? '#ff7777' : '#58eba6'" stroke-width="2" vector-effect="non-scaling-stroke" /></svg>`,
  styles: ':host { display:block; width:100%; height:34px; } svg { width:100%; height:100%; overflow:visible; }',
})
export class SparklineComponent {
  @Input({ required: true }) values: number[] = [];
  @Input() negative = false;

  points(): string {
    if (this.values.length < 2) return '';
    const min = Math.min(...this.values); const max = Math.max(...this.values); const range = max - min || 1;
    return this.values.map((value, index) => `${(index / (this.values.length - 1)) * 120},${32 - ((value - min) / range) * 30}`).join(' ');
  }
}

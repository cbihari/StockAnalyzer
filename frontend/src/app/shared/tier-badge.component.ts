import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-tier-badge',
  template: `<span [class.pro]="tier === 'Pro'" [class.power]="tier === 'Power'">{{ tier }}</span>`,
  styles: [`
    span { display: inline-flex; align-items: center; padding: 4px 8px; border: 1px solid #2a352e; border-radius: 20px; color: #87938b; background: rgba(255,255,255,.025); font-size: .6rem; font-weight: 800; letter-spacing: .08em; text-transform: uppercase; }
    span.pro { border-color: rgba(88,235,166,.28); color: #58eba6; background: rgba(88,235,166,.08); }
    span.power { border-color: rgba(112,177,255,.32); color: #70b1ff; background: rgba(112,177,255,.08); }
  `],
})
export class TierBadgeComponent {
  @Input() tier: 'Free' | 'Pro' | 'Power' = 'Free';
}

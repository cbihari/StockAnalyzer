import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-info-tip',
  template: `
    <span class="info-tip">
      <button type="button" class="info-tip-trigger" [attr.aria-label]="text">ⓘ</button>
      <span class="info-tip-text" role="tooltip">{{ text }}</span>
    </span>
  `,
  styles: [`
    :host { display: inline-flex; margin-left: 4px; vertical-align: middle; }
    .info-tip { position: relative; display: inline-flex; }
    .info-tip-trigger { display: inline-grid; place-items: center; width: 16px; height: 16px; padding: 0; border: 0; border-radius: 50%; color: #87938b; background: transparent; font-size: 12px; line-height: 1; cursor: help; }
    .info-tip-trigger:hover, .info-tip-trigger:focus-visible { color: #58eba6; outline: none; }
    .info-tip-text { position: absolute; z-index: 50; bottom: calc(100% + 8px); left: 50%; width: max-content; max-width: min(230px, 75vw); padding: 8px 10px; border: 1px solid #354139; border-radius: 7px; color: #f4f8f5; background: #131a16; box-shadow: 0 10px 28px rgba(0,0,0,.35); font-size: 11px; font-weight: 500; line-height: 1.4; text-align: left; text-transform: none; letter-spacing: 0; opacity: 0; pointer-events: none; transform: translate(-50%, 4px); transition: opacity .15s ease, transform .15s ease; }
    .info-tip:hover .info-tip-text, .info-tip:focus-within .info-tip-text { opacity: 1; transform: translate(-50%, 0); }
  `],
})
export class InfoTipComponent {
  @Input({ required: true }) text = '';
}

import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  imports: [RouterLink],
  template: `
    <main class="page hero-page">
      <p class="eyebrow">STOCK ANALYSIS, MADE CLEAR</p>
      <h1>Understand tomorrow's market signal.</h1>
      <p class="lead">Explore price history, technical indicators, and a simple machine-learning prediction from one friendly dashboard.</p>
      <div class="actions"><a class="button" routerLink="/search">Analyze a stock</a><a class="secondary" routerLink="/accuracy">View model results</a></div>
      <section class="feature-grid">
        <article class="card"><span>01</span><h3>Search</h3><p>Enter a Yahoo Finance ticker such as RELIANCE.NS.</p></article>
        <article class="card"><span>02</span><h3>Understand</h3><p>Review indicators and a clean closing-price chart.</p></article>
        <article class="card"><span>03</span><h3>Compare</h3><p>See UP or DOWN probability with the strongest model inputs.</p></article>
      </section>
    </main>`,
})
export class HomeComponent {}

import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-portal-home-page',
  standalone: true,
  imports: [RouterLink],
  template: `
    <main class="home-page">
      <section>
        <p class="eyebrow">S|2 PORTAL</p>
        <h1>Company portal</h1>
        <p>Choose an authorized area of the portal to continue.</p>
        <a routerLink="/users">User management</a>
      </section>
    </main>
  `,
  styles: `
    .home-page { display: grid; min-height: calc(100vh - 60px); place-items: center; padding: 24px; background: #f7f8fc; }
    section { width: min(100%, 560px); padding: 36px; border: 1px solid #e8ebf2; border-radius: 12px; background: #fff; }
    .eyebrow { color: #64748b; font-size: 11px; font-weight: 700; letter-spacing: 1.2px; }
    h1 { color: #172033; }
    p { color: #68758a; }
  `
})
export class PortalHomePage {}

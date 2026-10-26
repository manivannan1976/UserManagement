import { Component } from '@angular/core';

@Component({
  selector: 'app-auth-callback-page',
  standalone: true,
  template: `
    <main class="callback-page" role="status">
      <p>Completing sign-in...</p>
    </main>
  `,
  styles: `
    .callback-page {
      display: grid;
      min-height: 100vh;
      place-items: center;
      color: #52617a;
      font: 14px Roboto, "Helvetica Neue", sans-serif;
    }
  `
})
export class AuthCallbackPage {}

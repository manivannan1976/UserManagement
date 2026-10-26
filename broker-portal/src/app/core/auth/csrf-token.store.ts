import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class CsrfTokenStore {
  private requestToken: string | null = null;

  set(token: string): void {
    this.requestToken = token;
  }

  get(): string | null {
    return this.requestToken;
  }
}

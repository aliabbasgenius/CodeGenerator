import { inject, Injectable } from '@angular/core';
import { APP_CONFIG, AppConfig } from './app-config';

@Injectable({ providedIn: 'root' })
export class AppConfigService {
  private readonly config = inject(APP_CONFIG);

  get apiBaseUrl(): string {
    return this.config.apiBaseUrl;
  }
}

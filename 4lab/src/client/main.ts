import { bootstrapApplication } from '@angular/platform-browser';

import { App } from './app/app';
import { appConfig } from './app/core/config/app.config';

bootstrapApplication(App, appConfig).catch((err) => console.error(err));

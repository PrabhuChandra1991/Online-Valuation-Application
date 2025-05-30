/// <reference types="@angular/localize" />

import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';

import { importProvidersFrom } from '@angular/core';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';

// bootstrapApplication(AppComponent, appConfig)
//   .catch((err) => console.error(err));
  bootstrapApplication(AppComponent, {
    ...appConfig,
    providers: [
      ...(appConfig.providers || []),
      importProvidersFrom(BrowserAnimationsModule),
      provideToastr({
        closeButton: true,
        tapToDismiss: true,
        timeOut: 3000,
        progressBar: false
      })
    ]
  }).catch(err => console.error(err));
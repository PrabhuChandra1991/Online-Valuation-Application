import { Component, OnInit, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ThemeModeService } from './core/services/theme-mode.service';
import {MatProgressSpinnerModule} from '@angular/material/progress-spinner';
import { NgIf } from '@angular/common';
import { SpinnerService } from './views/pages/services/spinner.service';



@Component({
    selector: 'app-root',
    imports: [RouterOutlet, MatProgressSpinnerModule, NgIf],
    templateUrl: './app.component.html',
    styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  title = 'Student Valuation App';

  
  isLoaded:boolean = false;

  constructor(private themeModeService: ThemeModeService, 
              private spinnerService: SpinnerService) {}
  
  ngOnInit(): void {
    this.spinnerService.change.subscribe(emittedValue => {
      this.isLoaded = emittedValue;
    });
  }

}

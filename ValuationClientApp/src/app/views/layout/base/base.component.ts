import { Component, inject, OnInit } from '@angular/core';
import { RouteConfigLoadEnd, RouteConfigLoadStart, Router, RouterOutlet } from '@angular/router';
import { NavbarComponent } from '../navbar/navbar.component';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { FooterComponent } from '../footer/footer.component';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-base',
  imports: [
    RouterOutlet,
    NavbarComponent,
    SidebarComponent,
    FooterComponent,
    CommonModule
  ],
  templateUrl: './base.component.html',
  styleUrl: './base.component.scss'
})
export class BaseComponent implements OnInit {

  isLoading: boolean = false;
  private router = inject(Router);

  constructor() { }

  isMinimalRoute(): boolean {
    //const hiddenRoutes = ['/apps/viewevaluation', '/preview'];    
    //return hiddenRoutes.includes(this.router.url);
    return this.router.url.includes('/apps/viewevaluation');
  }

  ngOnInit(): void {
    // Spinner for lazy loading modules/components
    this.router.events.forEach((event) => {
      if (event instanceof RouteConfigLoadStart) {
        this.isLoading = true;
      } else if (event instanceof RouteConfigLoadEnd) {
        this.isLoading = false;
      }
    });
  }

}

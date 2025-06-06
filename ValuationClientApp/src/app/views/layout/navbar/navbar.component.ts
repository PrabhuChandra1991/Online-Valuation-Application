import { Component, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { NgbDropdownModule } from '@ng-bootstrap/ng-bootstrap';
import { ThemeModeService } from '../../../core/services/theme-mode.service';

@Component({
    selector: 'app-navbar',
    imports: [
        NgbDropdownModule,
        RouterLink
    ],
    templateUrl: './navbar.component.html',
    styleUrl: './navbar.component.scss'
})
export class NavbarComponent implements OnInit {

  currentTheme: string;

  userObj: any = {
    "email": '',
    "username":'',
    "mode":''
    // "createdDate":new Date().toJSON()
  }

  constructor(private router: Router, private themeModeService: ThemeModeService) {}

  ngOnInit(): void {
 
    const loggedData = localStorage.getItem('userData');

    if(loggedData)
    {
      const userData = JSON.parse(loggedData);

      this.userObj.email = userData.email;
      this.userObj.username = userData.name;
      this.userObj.mode=userData.mode;
    }

    this.themeModeService.currentTheme.subscribe( (theme) => {
      this.currentTheme = theme;
      this.showActiveTheme(this.currentTheme);
    });
  }

  showActiveTheme(theme: string) {
    const themeSwitcher = document.querySelector('#theme-switcher') as HTMLInputElement;
    const box = document.querySelector('.box') as HTMLElement;

    if (!themeSwitcher) {
      return;
    }

    // Toggle the custom checkbox based on the theme
    if (theme === 'dark') {
      themeSwitcher.checked = true;
      box.classList.remove('light');
      box.classList.add('dark');
    } else if (theme === 'light') {
      themeSwitcher.checked = false;
      box.classList.remove('dark');
      box.classList.add('light');
    }
  }

  /**
   * Change the theme on #theme-switcher checkbox changes
   */
  onThemeCheckboxChange(e: Event) {
    const checkbox = e.target as HTMLInputElement;
    const newTheme: string = checkbox.checked ? 'dark' : 'light';
    this.themeModeService.toggleTheme(newTheme);
    this.showActiveTheme(newTheme);
  }

  /**
   * Toggle the sidebar when the hamburger button is clicked
   */
  toggleSidebar(e: Event) {
    e.preventDefault();
    document.body.classList.add('sidebar-open');
    document.querySelector('.sidebar .sidebar-toggler')?.classList.add('active');
  }

  editProfile(){
    
    let loggedDataJson = localStorage.getItem('userData');
    if(loggedDataJson)
    {
      let loggedDataObject = JSON.parse(loggedDataJson);
      localStorage.setItem('isLoggedin', 'true');
      this.router.navigate(['/dashboard/edit', loggedDataObject.userId]); // Redirect to Edit
    }
     return;

  }
  /**
   * Logout
   */
  onLogout(e: Event) {
    e.preventDefault();

    localStorage.setItem('isLoggedin', 'false');

    if (localStorage.getItem('isLoggedin') === 'false') {
      localStorage.clear();
      this.router.navigate(['/auth/login']);
    }
  }

}

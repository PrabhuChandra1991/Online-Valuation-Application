import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router, RouterStateSnapshot } from '@angular/router';

export const authGuard: CanActivateFn = (route: ActivatedRouteSnapshot, state: RouterStateSnapshot) => {
  const router = inject(Router);

  if (localStorage.getItem('isLoggedin') === 'true') {
    const loginTimeStr = localStorage.getItem('loginTime');
    if (loginTimeStr) {
      const loginTime = new Date(loginTimeStr).getTime();
      const currentTime = new Date().getTime();
      const diffInMinutes = (currentTime - loginTime) / (1000 * 60); // milliseconds to minutes

      if (diffInMinutes > 2) {
        // Session expired
        localStorage.removeItem('loginTime'); // Optional
        router.navigateByUrl('/auth/login');
        return false;
      }

      return true;
    }
  }

  // If the user is not logged in, redirect to the login page with the return URL
  router.navigate(['/auth/login'], { queryParams: { returnUrl: state.url.split('?')[0] } });
  
  return false;
  
};

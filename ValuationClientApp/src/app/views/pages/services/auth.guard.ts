import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const authGuard: CanActivateFn = (route, state) => {

  const router = inject(Router);

  const localData = localStorage.getItem('isLoggedin');
 
  if(localData != null)
  {
    const loginTimeStr = localStorage.getItem('loginTime');
    if (loginTimeStr) {
      const loginTime = new Date(loginTimeStr).getTime();
      const currentTime = new Date().getTime();
      const diffInMinutes = (currentTime - loginTime) / (1000 * 60); // milliseconds to minutes

      if (diffInMinutes > 20) {
        // Session expired
        localStorage.removeItem('loginTime'); // Optional
        router.navigateByUrl('/auth/login');
        return false;
      }

      return true;
    }
  }
  else{
    router.navigateByUrl('/auth/login');
    return false;
  }
  return true;
};

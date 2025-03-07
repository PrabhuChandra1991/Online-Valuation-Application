import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const authGuard: CanActivateFn = (route, state) => {

  const router = inject(Router);

  const localData = localStorage.getItem('isLoggedin');

  debugger;

  if(localData != null)
  {
    return true;
  }
  else{
    router.navigateByUrl('/auth/login');
    return false;
  }
  return true;
};

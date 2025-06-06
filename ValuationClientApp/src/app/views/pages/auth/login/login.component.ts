import { NgIf, NgStyle } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ToastrModule,ToastrService  } from 'ngx-toastr';
import { SpinnerService } from '../../services/spinner.service';
import { LoginService } from '../../services/login.service';

@Component({
    selector: 'app-login',
    imports: [
        NgStyle,
        RouterLink,
        NgIf,
        FormsModule,
        ToastrModule,

    ],
    templateUrl: './login.component.html',
    styleUrl: './login.component.scss'
})
export class LoginComponent implements OnInit {

  returnUrl: any;

  isOTPRequested: boolean = false;
  userEmail:string='';
  userOtp ?:number;

  constructor(private router: Router, private route: ActivatedRoute,
    private toastr: ToastrService,private spinnerService: SpinnerService,
  private LoginService: LoginService) {}

  userObj: any = {
    "email": this.userEmail,
      "tempPassword":this.userOtp,
    // "createdDate":new Date().toJSON()
  }

  ngOnInit(): void {
    // Get the return URL from the route parameters, or default to '/'
    //this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';
  }

  // onLoggedin(e: Event) {
  //   e.preventDefault();
  //   localStorage.setItem('isLoggedin', 'true');
  //   if (localStorage.getItem('isLoggedin') === 'true') {
  //     this.router.navigate([this.returnUrl]);
  //   }
  // }

  requestPassword() {
    //e.preventDefault();
    this.spinnerService.toggleSpinnerState(true);
     //
    this.LoginService.requestPassword(this.userObj)
    .subscribe({
      next: (response) => {
        console.log(response);
        this.toastr.success(response['message']);
        this.isOTPRequested = true;
      },
      error: (res) => {
        console.log(res);
        this.toastr.error(res['error']['message']);
        this.spinnerService.toggleSpinnerState(false);
      },
      complete: () => {
        this.spinnerService.toggleSpinnerState(false);
       }
    });
    // .subscribe((result:any)=>{
    //  console.log(result);
    //   this.toastr.success(result['message']);
    //   this.isOTPRequested = true;
    //   this.spinnerService.toggleSpinnerState(false);
    // });

  }

  login() {
 
    this.spinnerService.toggleSpinnerState(true);
    this.LoginService.login(this.userObj)
    .subscribe({
      next: (response) => {
        console.log(response);
        localStorage.setItem('isLoggedin', 'true');
        localStorage.setItem('userData',JSON.stringify(response));
        // Set login time in localStorage when user logs in
        localStorage.setItem('loginTime', new Date().toISOString());
        //this.toastr.success(response['message']);
        this.isOTPRequested = true;

        if (localStorage.getItem('isLoggedin') === 'true') {
              if(response.roleId == 1)
                this.router.navigate(['/apps/user']);
              else
                this.router.navigate(['/apps/assigntemplate']);
               }
      },
      error: (res) => {
        console.log(res);
        this.toastr.error(res['error']['message']);
        this.spinnerService.toggleSpinnerState(false);
      },
      complete: () => {
        this.spinnerService.toggleSpinnerState(false);
       }
    })
    // .subscribe((result:any)=>{
    //   
    //   localStorage.setItem('isLoggedin', 'true');
    //   localStorage.setItem('userData',JSON.stringify(result));

    //   this.isOTPRequested = false;

    //   if (localStorage.getItem('isLoggedin') === 'true') {
    //     if(result.roleId == 1)
    //       this.router.navigate(['/apps/user']);
    //     else
    //       this.router.navigate(['/apps/assigntemplate']);
    //      }
    //      this.spinnerService.toggleSpinnerState(false);
    // });

  }

}

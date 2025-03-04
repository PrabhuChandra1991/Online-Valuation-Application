import { NgIf, NgStyle } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, Inject } from '@angular/core';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ToastrModule,ToastrService  } from 'ngx-toastr';

@Component({
    selector: 'app-register',
    imports: [
        NgStyle,
        RouterLink,
        FormsModule,
        NgIf,
        ToastrModule
        
        
    ],
    templateUrl: './register.component.html',
    styleUrl: './register.component.scss'
})
export class RegisterComponent {

  userForm: FormGroup = new FormGroup({
    userEmail: new FormControl("",[Validators.required,Validators.email,Validators.pattern("^\w+([\.-]?\w+)*@\w+([\.-]?\w+)*(\.\w{2,3})+$")])
    
  })

  

  constructor(private router: Router,private http: HttpClient,private toastr: ToastrService) {}

  userEmail:string='';
  userName:string='';
  userMobileNumber:string='';

  userObj: any = {
    "name":this.userName,
    "email": this.userEmail,
    "mobileNumber":this.userMobileNumber,
    "createdDate":new Date().toJSON()
}

  onRegister() {
    //e.preventDefault();
    
    debugger;
    //
    this.http.post("http://localhost:5088/api/User",this.userObj).subscribe((result:any)=>{
      if(result['id'] > 0)
      {
        this.toastr.success('User created!');
        
        this.userObj.name='';
        this.userObj.email='';
        this.userObj.mobileNumber='';
        
        this.router.navigate(['/auth/login']);
      }

    });

    //localStorage.setItem('isLoggedin', 'true');
    // if (localStorage.getItem('isLoggedin') === 'true') {
    //   this.router.navigate(['/']);
    // }
  }

}

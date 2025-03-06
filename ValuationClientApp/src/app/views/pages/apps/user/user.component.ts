import { Component,OnInit } from '@angular/core';
import { NgbDropdownModule, NgbNavModule, NgbTooltip, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { NgScrollbarModule } from 'ngx-scrollbar';

@Component({
  selector: 'app-user',
  imports: [ NgbNavModule,
          NgbDropdownModule,
          NgScrollbarModule,
          NgbTooltip
        ],
  templateUrl: './user.component.html',
  styleUrl: './user.component.scss'
})
export class UserComponent implements OnInit{
  
  ngOnInit(): void {
    
    // Get the return URL from the route parameters, or default to '/'
    //this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';
  }

}

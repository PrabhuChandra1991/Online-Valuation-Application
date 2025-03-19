import { AfterViewInit, OnInit, Component, TemplateRef, } from '@angular/core';
import { NgbDropdownModule, NgbNavModule, NgbTooltip, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { NgScrollbarModule } from 'ngx-scrollbar';
import { TemplateManagementService } from '../../../services/template-management.service';
import { CommonModule } from '@angular/common';
import { UserService } from '../../../services/user.service';

@Component({
  selector: 'app-template-assignment',
  imports: [
    NgbNavModule,
    NgbDropdownModule,
    NgScrollbarModule,
    NgbTooltip,
    CommonModule
],
  templateUrl: './template-assignment.component.html',
  styleUrl: './template-assignment.component.scss'
})
export class TemplateAssignmentComponent implements AfterViewInit {
  defaultNavActiveId = 1;
  basicModalCode: any;
    scrollableModalCode: any;
    verticalCenteredModalCode: any;
    optionalSizesModalCode: any;
  
    basicModalCloseResult: string = '';
    users: any[] = [];
    templates: any[] = [];

    selectedCourseId: any | null = null;
    qpTemplateData: any = null;
    constructor(private modalService: NgbModal,
      private templateService: TemplateManagementService,
      private userService: UserService
    ) { }
  
    ngOnInit(): void {
     
      this.loadTemplates();

      this.loadExperts();
    }

  ngAfterViewInit(): void {

    // Show the chat-content when a chat-item is clicked on tablet and mobile devices
    // document.querySelectorAll('.chat-list .chat-item').forEach(item => {
    //   item.addEventListener('click', event => {
    //     document.querySelector('.chat-content')!.classList.toggle('show');
    //   })
    // });

  }

  openAssignModal(content: TemplateRef<any>) {
      this.modalService.open(content, {size: 'lg'}).result.then((result) => {
        console.log("Modal closed" + result);
      }).catch((res) => {});
    }

  // Back to the chat-list on tablet and mobile devices
  // backToChatList() {
  //   document.querySelector('.chat-content')!.classList.toggle('show');
  // }

  loadExperts(): void {
    this.userService.getUsers().subscribe({
      next: (data) => {
        this.users = data;
        
        console.log('Users loaded:', this.users);
      },
      error: (error) => {
        console.error('Error loading users:', error);
      }
    });
  }

  loadTemplates(): void {
    this.templateService.getTemplates().subscribe({
      next: (data: any[]) => {
        this.templates = data;
       
        console.log('qp templated loaded:', this.templates);
      },
      error: (error) => {
        console.error('Error loading qp templated:', error);
      }
    });
  }
  save() {
    console.log('passs');
  }
}

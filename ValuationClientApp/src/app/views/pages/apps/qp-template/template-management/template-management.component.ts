
import { AfterViewInit, OnInit, Component, TemplateRef } from '@angular/core';
import { NgbDropdownModule, NgbNavModule, NgbTooltip, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { NgScrollbarModule } from 'ngx-scrollbar';
import { TemplateManagementService } from '../../../services/template-management.service';
import { CommonModule } from '@angular/common';


@Component({
  selector: 'app-template-list',
  imports: [
    NgbNavModule,
    NgbDropdownModule,
    NgScrollbarModule,
    NgbTooltip,
    CommonModule
],
  templateUrl: './template-management.component.html',
  styleUrl: './template-management.component.scss'
})
export class TemplateManagemenComponent implements OnInit, AfterViewInit {
  defaultNavActiveId = 1;
  basicModalCode: any;
    scrollableModalCode: any;
    verticalCenteredModalCode: any;
    optionalSizesModalCode: any;
  
    basicModalCloseResult: string = '';

    courses: any[] = [];
    selectedCourseId: any | null = null;
    qpTemplateData: any = null;
  
  constructor(private modalService: NgbModal,
    private templateService: TemplateManagementService
  ) {

  }
  
    ngOnInit(): void {
      this.loadCourses();
    }

  ngAfterViewInit(): void {

    // Show the chat-content when a chat-item is clicked on tablet and mobile devices
    // document.querySelectorAll('.chat-list .chat-item').forEach(item => {
    //   item.addEventListener('click', event => {
    //     document.querySelector('.chat-content')!.classList.toggle('show');
    //   })
    // });

  }

    openLgModal(content: TemplateRef<any>) {
      this.modalService.open(content, {size: 'lg'}).result.then((result) => {
        console.log("Modal closed" + result);
      }).catch((res) => {});
    }

    //#region template dialog functionalites

    loadCourses(): void {
      this.templateService.getCourses().subscribe({
        next: (data) => {
          this.courses = data;
          console.log('Courses loaded:', this.courses);
        },
        error: (error) => {
          console.error('Error loading courses:', error);
        }
      });
    }

    onCourseChange(event: Event): void {
       this.selectedCourseId = (event.target as HTMLSelectElement).value;
      this.fetchQPTemplate(this.selectedCourseId);
    }
  
    fetchQPTemplate(courseId: number): void {
      this.templateService.getQPTemplateByCourseId(courseId).subscribe((response) => {
        this.qpTemplateData = response;
        console.log("qpTemplate",this.qpTemplateData);
      });
    }

    //#endregion




  // Back to the chat-list on tablet and mobile devices
  // backToChatList() {
  //   document.querySelector('.chat-content')!.classList.toggle('show');
  // }

  save() {
    console.log('passs');
  }
}

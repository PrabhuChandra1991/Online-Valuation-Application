import { AfterViewInit, OnInit, Component, TemplateRef, ViewChild} from '@angular/core';
import { NgbDropdownModule, NgbNavModule, NgbTooltip, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { NgScrollbarModule } from 'ngx-scrollbar';
import { TemplateManagementService } from '../../../services/template-management.service';
import { CommonModule } from '@angular/common';
import { UserService } from '../../../services/user.service';
import { FormBuilder, FormGroup, FormArray, Validators, FormsModule, ReactiveFormsModule, FormControl } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { SpinnerService } from '../../../services/spinner.service';
import { Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';

@Component({
  selector: 'app-template-assignment',
  imports: [
    NgbNavModule,
    NgbDropdownModule,
    NgScrollbarModule,
    NgbTooltip,
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatTableModule, MatSortModule, MatPaginatorModule
],
  templateUrl: './template-assignment.component.html',
  styleUrl: './template-assignment.component.scss'
})
export class TemplateAssignmentComponent implements OnInit, AfterViewInit {

  isAdmin: Boolean = false;
  defaultNavActiveId = 1;
  basicModalCode: any;
    scrollableModalCode: any;
    verticalCenteredModalCode: any;
    optionalSizesModalCode: any;
    templateAssignmentForm!: FormGroup;
    basicModalCloseResult: string = '';
    users: any[] = [];
    templates: any[] = [];
   
    selectedCourseId: any | null = null;
    qpTemplateData: any = null;

    displayedColumns: string[] = ['qpTemplateName', 'documentName','qpeneration' ,'qpTemplateStatusTypeName'];
    dataSource = new MatTableDataSource<any>([]);
      @ViewChild(MatPaginator) paginator: MatPaginator;
        @ViewChild(MatSort) sort: MatSort;
    constructor(private modalService: NgbModal,
      private templateService: TemplateManagementService,
      private userService: UserService,
      private fb: FormBuilder,
      private toasterService: ToastrService,
      private spinnerService: SpinnerService,
      private router: Router
    ) { 
      this.templateAssignmentForm = this.fb.group({
        templateId: ['', Validators.required],
        userId: ['', Validators.required]
      });

    }
  
    ngOnInit(): void {
     
      this.loadTemplates();

      this.loadExperts();

      this.loadAssignedTemplates();

      const loggedInUser = localStorage.getItem('userData');

      if(loggedInUser)
      {
        const userData = JSON.parse(loggedInUser);

        this.isAdmin = userData.roleId == 1;
      }
      
    }

  ngAfterViewInit(): void {

    // Show the chat-coloadAssignedTemplatesntent when a chat-item is clicked on tablet and mobile devices
    // document.querySelectorAll('.chat-list .chat-item').forEach(item => {
    //   item.addEventListener('click', event => {
    //     document.querySelector('.chat-content')!.classList.toggle('show');
    //   })
    // });

  }

  applyFilter(event: Event) {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();

    if (this.dataSource.paginator) {
      this.dataSource.paginator.firstPage();
    }
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

  loadAssignedTemplates(): void {
    const loggedData = localStorage.getItem('userData');
    debugger;
    if (loggedData) {
      const userData = JSON.parse(loggedData);
      
      this.templateService.getAssignedQpTemplateByUser(userData.userId).subscribe({
        next: (data: any[]) => {
          this.templates = data;
          this.dataSource.data = this.templates; 
          this.dataSource.paginator = this.paginator;
         this.dataSource.sort = this.sort;
          console.log('assigned qp templated loaded:', this.templates);
        },
        error: (error) => {
          console.error('Error loading qp templated:', error);
        }
      });
    }

    
  }

  onSave() {
    if (this.templateAssignmentForm.valid) {
    
      this.spinnerService.toggleSpinnerState(true);
      const formData = this.templateAssignmentForm.value;
     
      this.templateService.assignQpTemplateToUser(formData.userId,formData.templateId).subscribe({
        next: (response) => {
          console.log('Assigned successful:', response);
          this.spinnerService.toggleSpinnerState(false);
          this.toasterService.success('Qp Template assigned successfully')
          this.modalService.dismissAll();
          
        },
        error: (error) => {
          console.error('Save failed:', error);
          this.toasterService.error('Save failed:', error)
          this.spinnerService.toggleSpinnerState(false);
        }
      });
    } else {
      this.toasterService.warning('Please fill in all required fields.');
    }
  }
}

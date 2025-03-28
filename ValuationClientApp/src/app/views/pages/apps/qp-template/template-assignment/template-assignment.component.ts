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
import {MatCheckboxModule} from '@angular/material/checkbox';

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
    MatFormFieldModule, MatInputModule, MatTableModule, MatSortModule, MatPaginatorModule,
    MatCheckboxModule
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
    courses: any[] = [];
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
      // this.templateAssignmentForm = this.fb.group({
      //   templateId: ['', Validators.required],
      //   userId: ['', Validators.required],
      //   qpTemplateName: [''],
      //   degreeTypeName: [{ value: '', disabled: true }],
      //   regulationYear: [{ value: '', disabled: true }],
      //   batchYear: [{ value: '', disabled: true }],
      //   examYear: [{ value: '', disabled: true }],
      //   examMonth: [{ value: '', disabled: true }],
      //   examType:  [{ value: '', disabled: true }],
      //   semester:  [{ value: '', disabled: true }],
      //   institutionName: [{ value: '', disabled: true }],
      //   studentCount: [{ value: '', disabled: true }],
      //   courseId: ['', Validators.required]
      // });

    }

    ngOnInit(): void {

      this.resetForm();
      
      this.loadCourses();

      this.initializeForm();

     // this.loadTemplates();

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
    this.templateService.getExpertsForQPAssignment().subscribe({
      next: (data) => {
        this.users = data.filter((user: any) => user.userId != 1);

        console.log('Users loaded:', this.users);
      },
      error: (error) => {
        console.error('Error loading users:', error);
      }
    });
  }

  loadTemplates(): void {
    this.templateService.getTemplatesByStatusId(1).subscribe({
      next: (data: any[]) => {
        this.templates = data;

        console.log('qp templated loaded:', this.templates);
      },
      error: (error) => {
        console.error('Error loading qp templated:', error);
      }
    });
  }
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

  onCourseChange(event: Event): void {
    this.selectedCourseId = (event.target as HTMLSelectElement).value;
   this.fetchQPTemplate(this.selectedCourseId);
 }

 fetchQPTemplate(courseId: number): void {
   this.templateService.getQPTemplateByCourseId(courseId).subscribe((response) => {
     this.qpTemplateData = response;
     //this.institutions = response.institutions;
    //  this.initializeForm(); 
    

     if(this.qpTemplateData)
     {
       this.templateAssignmentForm.patchValue({
         degreeTypeName: response.degreeTypeName,
         regulationYear: response.regulationYear,
         batchYear: response.batchYear,
         examYear: response.examYear,
         examMonth:  response.examMonth,
         examType: response.examType,
         semester: response.semester,
         institutionName: response.institutions[0]?.institutionName || '',
         studentCount:  response.institutions[0]?.studentCount || ''
       });

        this.templates =this.qpTemplateData.qpDocuments;

        this.updateFormWithData();

       console.log("qpTemplate",JSON.stringify(this.qpTemplateData));
     }
     //console.log("qpTemplate",JSON.stringify(this.qpTemplateData)  );
   });
 }

 updateFormWithData(): void {
  const qpDocumentsArray = this.templateAssignmentForm.get('qpDocuments') as FormArray;
  qpDocumentsArray.clear(); // Clear any existing data to avoid duplicates

  this.qpTemplateData.qpDocuments.forEach((qpDoc:any) => {
    qpDocumentsArray.push(this.createDocumentFormGroup(qpDoc));
  });
}

createDocumentFormGroup(qpDoc: any): FormGroup {
  return this.fb.group({
    qpDocumentId: [qpDoc.qpDocumentId],
    qpDocumentName: [qpDoc.qpDocumentName],
    qpAssignedUsers: this.fb.array(qpDoc.qpAssignedUsers.map((user:any) => this.createAssignedUserGroup(user)))
  });
}

createAssignedUserGroup(user: any): FormGroup {
  return this.fb.group({
    userId: [user.userId || ''],
    userName: [user.userName || ''],
    isQPOnly: [user.isQPOnly || false]
  });
}

updateIsQPOnly(docIndex: number, userIndex: number, isChecked: boolean) {
  this.qpTemplateData.qpDocuments[docIndex].qpAssignedUsers[userIndex].isQPOnly = isChecked;
  console.log("Updated isQPOnly:", this.qpTemplateData.qpDocuments);
}

 initializeForm() {

  this.resetForm();

  const qpDocumentsArray = this.templateAssignmentForm.get('qpDocuments') as FormArray;

  this.qpTemplateData?.qpDocuments?.forEach((doc:any) => {
    
    const assignedUsersArray = this.fb.array<FormGroup>([]); 

    doc.qpAssignedUsers.forEach((user:any) => {
      assignedUsersArray.push(
        this.fb.group({
          userId: [user.userId || '', Validators.required], // Dropdown selection
          isQPOnly: [user.isQPOnly || false] // Checkbox
        })
      );
    });

    qpDocumentsArray.push(
      this.fb.group({
        qpDocumentName: [doc.qpDocumentName], // Document name
        qpAssignedUsers: assignedUsersArray // Nested FormArray
      })
    );
  });

}
  resetForm() {

    this.templateAssignmentForm = this.fb.group({
      qpTemplateName: [''],
      degreeTypeName: [{ value: '', disabled: true }],
      regulationYear: [{ value: '', disabled: true }],
      batchYear: [{ value: '', disabled: true }],
      examYear: [{ value: '', disabled: true }],
      examMonth: [{ value: '', disabled: true }],
      examType: [{ value: '', disabled: true }],
      semester: [{ value: '', disabled: true }],
      institutionName: [{ value: '', disabled: true }],
      studentCount: [{ value: '', disabled: true }],
      courseId: ['', Validators.required],
      templateId: [''],
      userId: [''],
      qpDocuments: this.fb.array([
        this.fb.group({
          qpAssignedUsers: this.fb.array([]) // Ensure this array exists
        })
      ])
    });
  }

updateUserDetails(docIndex: number, userIndex: number, user:any) {
console.log('templateForm',this.templateAssignmentForm);
  const selectedUserId = user.target?.value;
  const selectedUser = this.users.find(user => user.userId == selectedUserId);

  if (selectedUser) {
    this.qpTemplateData.qpDocuments[docIndex].qpAssignedUsers[userIndex].userId = selectedUser.userId;
    this.qpTemplateData.qpDocuments[docIndex].qpAssignedUsers[userIndex].userName = selectedUser.userName;
  } else {
    this.qpTemplateData.qpDocuments[docIndex].qpAssignedUsers[userIndex].userId = null;
    this.qpTemplateData.qpDocuments[docIndex].qpAssignedUsers[userIndex].userName = '';
  }

}

isUserAlreadySelected(qpAssignedUsers: any[], userId: number, currentIndex: number): boolean {
  return qpAssignedUsers.some((user, index) => index !== currentIndex && user.userId === userId);
}

  onSave() {
    if (this.templateAssignmentForm.valid) {
      
      this.spinnerService.toggleSpinnerState(true);
      const formData = this.qpTemplateData;
     console.log('final form data',JSON.stringify(formData));

      this.templateService.CreateQpTemplate(formData).subscribe({
        next: (response) => {
          console.log('Assigned successful:', response);
          this.loadAssignedTemplates();
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



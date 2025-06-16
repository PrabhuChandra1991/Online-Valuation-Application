import {
  AfterViewInit,
  OnInit,
  Component,
  TemplateRef,
  ViewChild,
  ElementRef,
} from '@angular/core';
import {
  NgbDropdownModule,
  NgbNavModule,
  NgbTooltip,
  NgbModal,
  NgbModalRef,
} from '@ng-bootstrap/ng-bootstrap';
import { NgScrollbarModule } from 'ngx-scrollbar';
import { TemplateManagementService } from '../../../services/template-management.service';
import { CommonModule } from '@angular/common';
import { UserService } from '../../../services/user.service';
import {
  FormBuilder,
  FormGroup,
  FormArray,
  Validators,
  FormsModule,
  ReactiveFormsModule,
  FormControl,
} from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { SpinnerService } from '../../../services/spinner.service';
import { ActivatedRoute, Router } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { InstituteService } from '../../../services/institute.service';
import { MatButtonModule } from '@angular/material/button';
import { QPDocumentService } from '../../../services/qpdocument.service';
import { AnswersheetService } from '../../../services/answersheet.service';

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
    MatFormFieldModule,
    MatInputModule,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatCheckboxModule,
    MatIconModule,
    MatButtonModule,
  ],
  templateUrl: './template-assignment.component.html',
  styleUrl: './template-assignment.component.scss',
})
export class TemplateAssignmentComponent implements OnInit, AfterViewInit {
  [x: string]: any;

  dataSourceExamYears: any[] = [];
  dataSourceExamMonths: any[] = [];
  dataSourceExamTypes: any[] = [];

  selectedExamYear: string = '0';
  selectedExamMonth: string = '0';
  selectedExamType: string = '0';

  ddlType = new FormControl('0');



  isAdmin: Boolean;
  isEditMode: Boolean;
  defaultNavActiveId = 1;
  basicModalCode: any;
  scrollableModalCode: any;
  verticalCenteredModalCode: any;
  optionalSizesModalCode: any;
  templateAssignmentForm!: FormGroup;
  basicModalCloseResult: string = '';
  users: any[] = [];
  templates: any[] = [];

  isCourseSyllabusDocuploaded: boolean = false;
  isQPPendingForScrutiny: boolean = false;

  selectedCourseId: any | null = null;
  selectedInstituteId: any | null = null;

  qpTemplateData: any = null;
  courses: any[] = [];
  institutes: any[] = [];
  institutionId: number;
  displayedColumns: string[] = [
    'qpTemplateName',
    'courseCode',
    'courseName',
    'qpTemplateStatusTypeName',
    'actions',
  ];
  dataSource = new MatTableDataSource<any>([]);
  @ViewChild(MatPaginator) paginator: MatPaginator;
  @ViewChild(MatSort) sort: MatSort;
  selectedAssignment: any | null;
  modalRef!: NgbModalRef;
  @ViewChild('assignmentModal') assignmentModal: any;
  @ViewChild('course') courseInput!: ElementRef;
  @ViewChild('graphName') graphName: ElementRef;
  @ViewChild('tableName') tableName: ElementRef;

  qpDocDataForm = new FormGroup({
    name: new FormControl('', [Validators.required, Validators.minLength(3)]),
    file: new FormControl('', [Validators.required]),
    fileSource: new FormControl('', [Validators.required]),
  });

  isQPDocUploaded: boolean = false;
  isQPDocValidated: boolean = false;
  isInvalidDoc: boolean = false;
  qpTemplateId: number = 0;
  qpValidationMessage: any;
  isGraphsRequired: boolean = false;
  isTablesAllowed: boolean = false;

  constructor(
    private modalService: NgbModal,
    private templateService: TemplateManagementService,
    private userService: UserService,
    private fb: FormBuilder,
    private toasterService: ToastrService,
    private spinnerService: SpinnerService,
    private router: Router,
    private instituteService: InstituteService,
    private toastr: ToastrService,
    private route: ActivatedRoute,
    private qpDocumentService: QPDocumentService,
    private answersheetService: AnswersheetService 
  ) { }

  ngOnInit(): void {
    this.loadAllExamYears();
    this.loadAllExamMonths();
    this.loadAllExamTypes();
    
    this.resetForm();

    this.loadCourses();

    this.initializeForm();

    this.loadExperts();

    this.loadAllInstitues();

    const loggedInUser = localStorage.getItem('userData');

    if (loggedInUser) {
      const userData = JSON.parse(loggedInUser);

      this.isAdmin = userData.roleId == 1;

      console.log("isAdmin: ", this.isAdmin)
      if (this.isAdmin) {
        // console.log('selected institute',this.selectedInstituteId);
        this.loadTemplateaForInstitute(0);
      } else {
        this.loadAssignedTemplates();
      }
    }
  }

  loadAllExamYears(): void {
    this.answersheetService.getExamYears().subscribe({
      next: (data: any) => {
        this.dataSourceExamYears = data;
        console.log('ExamYears loaded:', this.dataSourceExamYears);
      },
      error: (err: any) => {
        console.error('Error loading ExamYears:', err);
      },
    });
  }

  loadAllExamMonths(): void {
    this.answersheetService.getExamMonths().subscribe({
      next: (data: any) => {
        this.dataSourceExamMonths = data;
        console.log('ExamMonths loaded:', this.dataSourceExamMonths);
      },
      error: (err: any) => {
        console.error('Error loading ExamMonths:', err);
      },
    });
  }

  loadAllExamTypes(): void {
    this.answersheetService.getExamTypes().subscribe({
      next: (data: any) => {
        this.dataSourceExamTypes = data;
        console.log('ExamTypes loaded:', this.dataSourceExamMonths);
      },
      error: (err: any) => {
        console.error('Error loading ExamTypes:', err);
      },
    });
    this.ddlType.setValue('0');
  }

   onExamYearChange(event: Event): void {
    this.selectedExamYear = (event.target as HTMLSelectElement).value;
    this.ddlType.setValue('0');
  }

  onExamMonthChange(event: Event): void {
    this.selectedExamMonth = (event.target as HTMLSelectElement).value;
    this.ddlType.setValue('0');
  }

  onExamTypeChange(event: Event): void {
    this.selectedExamType = (event.target as HTMLSelectElement).value;
    //this.loadGridData();
    const loggedInUser = localStorage.getItem('userData');

    if (loggedInUser) {
      const userData = JSON.parse(loggedInUser);

      this.isAdmin = userData.roleId == 1;

      console.log("isAdmin: ", this.isAdmin)
      if (this.isAdmin) {
        // console.log('selected institute',this.selectedInstituteId);
        this.loadTemplateaForInstitute(0);
      } else {
        this.loadAssignedTemplates();
      }
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
    // this.isEditMode = false;
    // this.selectedAssignment = null;
    // this.modalRef = this.modalService.open(this.assignmentModal, { size: 'lg', backdrop: 'static' });
    this.modalService.open(content, { size: 'lg' }).result.then((result) => {
      console.log("Modal closed" + result);
    }).catch((res) => { });
  }
  courseCode:any;
  openDocUploadModal(content: TemplateRef<any>, qpTemplateId: number, courseCode:any) {
    this.qpTemplateId = qpTemplateId;
   this.courseCode = courseCode;
  this.qpDocDataForm = new FormGroup({
    name: new FormControl('', [Validators.required, Validators.minLength(3)]),
    file: new FormControl('', [Validators.required]),
    fileSource: new FormControl('', [Validators.required])
  });
  this.qpValidationMessage = "";
    this.isQPDocValidated  = false;
    this.isQPDocUploaded  = false;
    this.isInvalidDoc = false;

      this.modalService.open(content, {size: 'lg'}).result.then((result) => {
        console.log("Modal closed" + result);
      }).catch((res) => {});

    }

  editAssignment(content: TemplateRef<any>, documentId: any) {
    this.isEditMode = true; // Set edit mode
    this.getAssignmentForTemplateId(documentId);

    this.modalService
      .open(content, { size: 'lg' })
      .result.then((result) => {
        console.log('Modal closed' + result);
      })
      .catch((res) => { });
  }

  loadExperts(): void {
    this.templateService.getExpertsForQPAssignment().subscribe({
      next: (data) => {
        this.users = data.filter((user: any) => user.userId != 1);

        console.log('Users loaded:', this.users);
      },
      error: (error) => {
        console.error('Error loading users:', error);
      },
    });
  }

  loadAllInstitues(): void {
    this.instituteService.getInstitutions().subscribe({
      next: (data) => {
        this.institutes = data;

        console.log('All Institutes loaded:', data[0]?.institutionId);

        this.selectedInstituteId = data[0]?.institutionId;

        console.log('Institutes loaded:', this.institutes);
      },
      error: (error) => {
        console.error('Error loading Institutes:', error);
      },
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
      },
    });
  }

  loadTemplateaForInstitute(institutionId: number): void {
    //let institutionId = 2;
    this.spinnerService.toggleSpinnerState(true);
    this.templateService.getTemplatesData(institutionId,
        this.selectedExamYear,
        this.selectedExamMonth,
        this.selectedExamType
    ).subscribe({
      next: (data: any[]) => {
        this.dataSource.data = data;
        this.dataSource.paginator = this.paginator;
        this.dataSource.sort = this.sort;
        console.log('qp templated loaded:', JSON.stringify(data));
      },
      error: (error) => {
        console.error('Error loading qp templated:', error);
      },
    });
    this.spinnerService.toggleSpinnerState(false);
  }

  loadCourses(): void {
    this.templateService.getCourses().subscribe({
      next: (data) => {
        this.courses = data;

        console.log('Courses loaded:', this.courses);
      },
      error: (error) => {
        console.error('Error loading courses:', error);
      },
    });
  }

  loadAssignedTemplates(): void {
    const loggedData = localStorage.getItem('userData');

    if (loggedData) {
      const userData = JSON.parse(loggedData);

      this.templateService
        .getAssignedQpTemplateByUserData(userData.userId,
          this.selectedExamYear,
         this.selectedExamMonth,
         this.selectedExamType
        )
        .subscribe({
          next: (data: any[]) => {
            this.templates = data;
            this.dataSource.data = this.templates;
            this.dataSource.paginator = this.paginator;
            this.dataSource.sort = this.sort;
            console.log('assigned qp templated loaded:', this.templates);
          },
          error: (error) => {
            console.error('Error loading qp templated:', error);
          },
        });
    }
  }

  onCourseChange(event: Event): void {
    this.selectedCourseId = (event.target as HTMLSelectElement).value;
    this.fetchQPTemplate(this.selectedCourseId);
  }

  onInstituteChange(event: Event): void {
    this.selectedInstituteId = 0
    this.loadTemplateaForInstitute(this.selectedInstituteId);
  }

  getAssignmentForTemplateId(templateId: any) {
    this.templateService.getQpTemplateById(templateId).subscribe((response) => {
      this.qpTemplateData = response;
      this.qpTemplateId = this.qpTemplateData.qpTemplateId;
      console.log('assigned template for sel template ', response);
      if (this.qpTemplateData) {
        for (
          let index = 0;
          index < this.qpTemplateData.qpDocuments.length;
          index++
        ) {
          const qpDocument = this.qpTemplateData.qpDocuments[index];
          for (
            let qpAIndex = 0;
            qpAIndex < qpDocument.qpAssignedUsers.length;
            qpAIndex++
          ) {
            const qpAssignedUser = qpDocument.qpAssignedUsers[qpAIndex];

            const selItems = qpDocument.qpScrutinityUsers.filter(
              (x: any) =>
                x.parentUserQPTemplateId == qpAssignedUser.userQPTemplateId
            );

            if (selItems.length === 0 && qpAssignedUser.statusTypeId == 9) {
              qpDocument.qpScrutinityUsers.push({
                userQPTemplateId: 0,
                institutionId: qpAssignedUser.institutionId,
                isQPOnly: qpAssignedUser.isQPOnly,
                parentUserQPTemplateId: qpAssignedUser.userQPTemplateId,
                qpTemplateId: 0,
                statusTypeId: 0,
                statusTypeName: '',
                submittedQPDocumentId: 0,
                submittedQPDocumentName: '',
                submittedQPDocumentUrl: '',
                expertUsertId: qpAssignedUser.userId,
              });
            }
          }
        }

        //this.courseInput.nativeElement.value = this.qpTemplateData?.courseId;
        this.templateAssignmentForm.patchValue({
          courseId: response.courseId,
          degreeTypeName: response.degreeTypeName,
          regulationYear: response.regulationYear,
          batchYear: response.batchYear,
          examYear: response.examYear,
          examMonth: response.examMonth,
          examType: response.examType,
          semester: response.semester,
          //institutionName: response.institutions[0]?.institutionName || '',
          studentCount: response?.studentCount || '',
          courseSyllabusDocumentName: response.courseSyllabusDocumentName,
          courseSyllabusDocumentUrl: response.courseSyllabusDocumentUrl,
        });

        this.templates = this.qpTemplateData.qpDocuments;

        this.updateFormWithData();

        this.isCourseSyllabusDocuploaded =
          this.qpTemplateData.courseSyllabusDocumentId > 0;

        this.isQPPendingForScrutiny =
          this.qpTemplateData.qpTemplateStatusTypeId == 3; // scrutiny assignment is pending

        this.isEditMode = false;

        console.log('qpTemplate', JSON.stringify(this.qpTemplateData));
      }
    });
  }

  fetchQPTemplate(courseId: number): void {
    this.templateService
      .getQPTemplateByCourseId(courseId)
      .subscribe((response) => {
        this.qpTemplateData = response;

        console.log(response);

        if (this.qpTemplateData) {
          this.templateAssignmentForm.patchValue({
            degreeTypeName: response.degreeTypeName,
            regulationYear: response.regulationYear,
            batchYear: response.batchYear,
            examYear: response.examYear,
            examMonth: response.examMonth,
            examType: response.examType,
            semester: response.semester,
            //  institutionName: response.institutions[0]?.institutionName || '',
            studentCount: response?.studentCount || '',
            courseSyllabusDocumentName: response.courseSyllabusDocumentName,
            courseSyllabusDocumentUrl: response.courseSyllabusDocumentUrl,
          });

          this.templates = this.qpTemplateData.qpDocuments;

          this.isCourseSyllabusDocuploaded =
            this.qpTemplateData.courseSyllabusDocumentId > 0;

          console.log(response.courseSyllabusDocumentName);

          this.updateFormWithData();

          console.log('qpTemplate', JSON.stringify(this.qpTemplateData));
        }
        //console.log("qpTemplate",JSON.stringify(this.qpTemplateData)  );
      });
  }

  updateFormWithData(): void {
    const qpDocumentsArray = this.templateAssignmentForm.get(
      'qpDocuments'
    ) as FormArray;
    qpDocumentsArray.clear(); // Clear any existing data to avoid duplicates

    this.qpTemplateData.qpDocuments.forEach((qpDoc: any) => {
      qpDocumentsArray.push(this.createDocumentFormGroup(qpDoc));
    });
  }

  createDocumentFormGroup(qpDoc: any): FormGroup {
    return this.fb.group({
      qpDocumentId: [qpDoc.qpDocumentId],
      qpDocumentName: [qpDoc.qpDocumentName],
      qpAssignedUsers: this.fb.array(
        qpDoc.qpAssignedUsers.map((user: any) =>
          this.createAssignedUserGroup(user)
        )
      ),
      qpScrutinityUsers: this.fb.array(
        qpDoc.qpScrutinityUsers.map((user: any) =>
          this.createScrutinityUserGroup(user)
        )
      ),
    });
  }

  createAssignedUserGroup(user: any): FormGroup {
    return this.fb.group({
      userQPTemplateId: [user.userQPTemplateId || 0],
      userId: [user.userId || ''],
      userName: [user.userName || ''],
      isQPOnly: [user.isQPOnly || false],
      statusTypeId: [user.statusTypeId || 0],
      statusTypeName: [user.statusTypeName || ''],
      parentUserQPTemplateId: null,
    });
  }

  createScrutinityUserGroup(user: any): FormGroup {
    return this.fb.group({
      userQPTemplateId: [user.userQPTemplateId || 0],
      userId: [user.userId || ''],
      userName: [user.userName || ''],
      isQPOnly: [user.isQPOnly || false],
      statusTypeId: [user.statusTypeId || 0],
      statusTypeName: [user.statusTypeName || ''],
      parentUserQPTemplateId: [user.parentUserQPTemplateId || ''],
    });
  }

  updateIsQPOnly(docIndex: number, userIndex: number, isChecked: boolean) {
    this.qpTemplateData.qpDocuments[docIndex].qpAssignedUsers[
      userIndex
    ].isQPOnly = isChecked;
    console.log('Updated isQPOnly:', this.qpTemplateData.qpDocuments);
  }

  initializeForm() {
    this.resetForm();

    const qpDocumentsArray = this.templateAssignmentForm.get(
      'qpDocuments'
    ) as FormArray;

    this.qpTemplateData?.qpDocuments?.forEach((doc: any) => {
      const assignedUsersArray = this.fb.array<FormGroup>([]);

      doc.qpAssignedUsers.forEach((user: any) => {
        assignedUsersArray.push(
          this.fb.group({
            userId: [user.userId || '', Validators.required], // Dropdown selection
            isQPOnly: [user.isQPOnly || false], // Checkbox
          })
        );
      });

      qpDocumentsArray.push(
        this.fb.group({
          qpDocumentName: [doc.qpDocumentName], // Document name
          qpAssignedUsers: assignedUsersArray, // Nested FormArray
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
      scrutinyUserId: [''],
      courseSyllabusDocumentName: [''],
      courseSyllabusDocumentUrl: [''],
      qpDocuments: this.fb.array([
        this.fb.group({
          qpAssignedUsers: this.fb.array([]), // Ensure this array exists
        }),
      ]),
    });
  }

  updateUserDetails(docIndex: number, userIndex: number, user: any) {
    console.log('templateForm', this.templateAssignmentForm);
    const selectedUserId = user.target?.value;
    const selectedUser = this.users.find(
      (user) => user.userId == selectedUserId
    );

    //docIndex = docIndex -1;

    if (selectedUser) {
      this.qpTemplateData.qpDocuments[docIndex].qpAssignedUsers[
        userIndex
      ].userId = selectedUser.userId;
      this.qpTemplateData.qpDocuments[docIndex].qpAssignedUsers[
        userIndex
      ].userName = selectedUser.userName;
    } else {
      this.qpTemplateData.qpDocuments[docIndex].qpAssignedUsers[
        userIndex
      ].userId = null;
      this.qpTemplateData.qpDocuments[docIndex].qpAssignedUsers[
        userIndex
      ].userName = '';
    }
  }

  updateScrutinyUserDetails(docIndex: number, userIndex: number, user: any) {
    console.log('templateForm', this.templateAssignmentForm);
    const selectedScrutinyUserId = user.target?.value;
    const selectedUser = this.users.find(
      (user) => user.userId == selectedScrutinyUserId
    );

    //docIndex = docIndex -1;

    if (selectedUser) {
      this.qpTemplateData.qpDocuments[docIndex].qpScrutinityUsers[
        userIndex
      ].userId = selectedUser.userId;
      this.qpTemplateData.qpDocuments[docIndex].qpScrutinityUsers[
        userIndex
      ].userName = selectedUser.userName;
    } else {
      this.qpTemplateData.qpDocuments[docIndex].qpScrutinityUsers[
        userIndex
      ].userId = null;
      this.qpTemplateData.qpDocuments[docIndex].qpScrutinityUsers[
        userIndex
      ].userName = '';
    }
  }

  getScrutinyUsers(assignedScrutinyUser: any) {
    return this.users.filter(
      (user: any) => user.userId != assignedScrutinyUser.expertUsertId
    );
  }

  isUserAlreadySelected(
    qpAssignedUsers: any[],
    userId: number,
    currentIndex: number
  ): boolean {
    return qpAssignedUsers.some(
      (user, index) => index !== currentIndex && user.userId === userId
    );
  }

  onSave() {
    if (this.templateAssignmentForm.valid) {
      if (this.isEditMode) {
        this.updateAssignment(this.qpTemplateData);
      } else {
        this.spinnerService.toggleSpinnerState(true);
        const formData = this.qpTemplateData;
        // formData.expert1Name = "";
        // formData.expert2Name = "";
        // formData.expert1Status = "";
        // formData.expert2Status = "";

        console.log('final form data', JSON.stringify(formData));

        this.templateService.CreateQpTemplate(formData).subscribe({
          next: (response) => {
            console.log('Assigned successful:', response);
            this.loadAssignedTemplates();
            this.spinnerService.toggleSpinnerState(false);
            this.toasterService.success('Qp Template assigned successfully')
            this.modalService.dismissAll();
            this.loadTemplateaForInstitute(0);
            window.location.reload();
          },
          error: (error) => {
            console.error('Save failed:', error);
            this.toasterService.error('Save failed:', error)
            this.spinnerService.toggleSpinnerState(false);
          }
        });
      }
    } else {
      this.toasterService.warning('Please fill in all required fields.');
    }
  }

  updateAssignment(assignmentData: FormData) {
    if (!this.selectedAssignment) return;

    //this.isSubmitting = true;

    this.templateService.updateAssignment(assignmentData.get('qpDocumentId'), assignmentData).subscribe({
      next: () => {
        this.toastr.success('User updated successfully!');
        this.loadAssignedTemplates();
        this.modalRef.close();
        window.location.reload();
      },
      error: (res) => {
        this.toastr.error(res['error']['message']);
        this.spinnerService.toggleSpinnerState(false);
      },
      complete: () => {
        // this.isSubmitting = false;
        this.spinnerService.toggleSpinnerState(false);
      }
    });

  }

  closeModal() {
    if (this.modalRef) {
      this.modalRef.close();
    }
  }
  openPdfInTab(base64String: string, filename: string, contentType:string) {
    const byteCharacters = atob(base64String);
    const byteNumbers = new Array(byteCharacters.length);
  
    for (let i = 0; i < byteCharacters.length; i++) {
      byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
  
    const byteArray = new Uint8Array(byteNumbers);
    const blob = new Blob([byteArray], { type: contentType });
    const blobUrl = URL.createObjectURL(blob);
  
    // Create a temporary anchor to download with filename
    const link = document.createElement('a');
    link.href = blobUrl;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  
    // Optionally, open in new tab too
    //window.open(blobUrl, '_blank');
  }
  previewDone:any = false;
  printQPDocument(userqpTemplateId: number, isForPrint: boolean) {
    this.spinnerService.toggleSpinnerState(true);
    this.templateService.printQPTemplate(userqpTemplateId, isForPrint).subscribe({
      next: (response) => {
        if (response.base64Content != "") {
          this.openPdfInTab(response.base64Content, response.fileName,response.contentType);
          this.toastr.success('File downloaded successfully!');
          if (isForPrint) {
            this.close()
            window.location.reload();
          }

        } else {
          this.toastr.error('No file found to print!');
        }
      },
      error: (response) => {
        this.toastr.error('No file found to print!');
        this.spinnerService.toggleSpinnerState(false);
      },
      complete: () => {
        // this.isSubmitting = false;
        this.spinnerService.toggleSpinnerState(false);
      }
    });
  }

  assignScrutinity(assignedScrutinyUser: any) {
    this.spinnerService.toggleSpinnerState(true);
    this.templateService.assignScrutinity(assignedScrutinyUser.userId, assignedScrutinyUser.parentUserQPTemplateId).subscribe({
      next: () => {
        this.close();
        this.spinnerService.toggleSpinnerState(false);
        window.location.reload();
      },
      error: (res) => {
        this.close();
        this.spinnerService.toggleSpinnerState(false);
      },
      complete: () => {
        this.close();
        this.spinnerService.toggleSpinnerState(false);
      }
    });

  }

  clearSelection(docIndex: number, userIndex: number) {
    const qpAssignedUsers = this.templateAssignmentForm.get(
      'qpDocuments'
    ) as FormArray;
    const userControl = qpAssignedUsers
      .at(docIndex)
      .get('qpAssignedUsers') as FormArray;

    // Reset userId dropdown & checkbox
    userControl.at(userIndex).patchValue({
      userId: '',
      isQPOnly: false,
    });
  }

  handleFormSubmit(assignmentData: any) {
    if (this.isEditMode) {
      // Update logic
      // Merge only changed values into selectedUser
      const updatedUser = { ...this.selectedAssignment, ...assignmentData };
      this.updateAssignment(updatedUser);
      console.log('Updating User:', updatedUser);
    } else {
      // Create logic
      //this.createUser(userData);
    }
  }

  close() {
    //this.resetForm();
    this.modalService.dismissAll();
  }

  onQPDocDataFileChange(event: any) {
    if (event.target.files.length > 0) {
      this.isQPDocUploaded = true;
      this.qpDocDataForm.patchValue({
        fileSource: event.target.files[0],
      });
    }
  }

  validate(qpDocumentId: number) {
    this.spinnerService.toggleSpinnerState(true);
    const formData = new FormData();
    const fileSourceValue = this.qpDocDataForm.get('fileSource')?.value;

    if (fileSourceValue !== null && fileSourceValue !== undefined) {
      formData.append('file', fileSourceValue);
    }

    this.qpDocumentService.validateQPFile(formData, qpDocumentId, this.courseCode).subscribe({
      next: (response) => {
        if (response.inValid) {
          this.qpValidationMessage = response.message;
          this.isQPDocValidated = false;
        } else {
          this.toastr.success('Data validated successfully!');
          //this.qpDocDataFormF['file'].setValue([]);
          this.qpValidationMessage = response.message;
          this.isQPDocValidated = true;
        }
      },
      error: (response) => {
        console.log(response);
        if (response.inValid) {
          this.qpValidationMessage = response.message;
          this.isQPDocValidated = false;
        }
        this.toastr.error('Invalid data. Please try again.');
        this.spinnerService.toggleSpinnerState(false);
      },
      complete: () => {
        this.spinnerService.toggleSpinnerState(false);
      },
    });
  }

  showTextBox(event: any) {
    if (event.srcElement.name == 'graph') {
      this.isGraphsRequired = event.target['checked'];
    } else if (event.srcElement.name == 'table') {
      this.isTablesAllowed = event.target['checked'];
    }
  }

  previewGeneratedDcument(userqpTemplateId: number) {
    this.previewDone = true;
    this.spinnerService.toggleSpinnerState(true);
    const formData = new FormData();
    const fileSourceValue = this.qpDocDataForm.get('fileSource')?.value;

    if (fileSourceValue !== null && fileSourceValue !== undefined) {
      formData.append('file', fileSourceValue);
    }

    this.qpDocumentService.generatedQPPreview(formData, userqpTemplateId).subscribe({
      next: (response) => {
        if (response.base64Content != "") {
          this.openPdfInTab(response.base64Content, response.fileName,response.contentType);
          this.toastr.success('File downloaded successfully!');
        } else {
          this.toastr.error('No file found to print!');
        }
        this.spinnerService.toggleSpinnerState(false);
      },
      error: (response) => {
        this.toastr.error('No file found to print!');
        this.spinnerService.toggleSpinnerState(false);
      },
      complete: () => {
        // this.isSubmitting = false;
        this.spinnerService.toggleSpinnerState(false);
      }
    });
  }

  submit(qpDocumentId: number) {
    if(!this.previewDone)
    {
        this.validationMessage = 'Please preview and verify details before submit.';
        this.previewDone = false;
        return;
    }
    this.spinnerService.toggleSpinnerState(true);
    const formData = new FormData();
    const fileSourceValue = this.qpDocDataForm.get('fileSource')?.value;

    if (fileSourceValue !== null && fileSourceValue !== undefined) {
      formData.append('file', fileSourceValue);
    }

    let graphName = '';
    let tableName = '';
    if (this.isGraphsRequired || this.isTablesAllowed) {
      if (this.graphName) {
        graphName = this.graphName.nativeElement.value;
      }
      if (this.tableName) {
        tableName = this.tableName.nativeElement.value;
      }
    }
    //console.log('submitted...');
    //console.log(this.isGraphsRequired + ': ' + graphName);
    //console.log(this.isTablesAllowed + ': ' + tableName);
    this.qpDocumentService.submitGeneratedQP(formData, qpDocumentId, this.isGraphsRequired, graphName, this.isTablesAllowed, tableName)
      .subscribe({
        next: (response) => {
          //console.log(response)
          if (response) {
            this.toastr.success('Data submitted successfully!');
            this.close();
            window.location.reload();
          }
        },
        error: (response) => {
          //console.log(response);
          this.toastr.error('Invalid data. Please try again.');
          this.spinnerService.toggleSpinnerState(false);
        },
        complete: () => {
          this.spinnerService.toggleSpinnerState(false);
        }
      });
  }
}


import { AfterViewInit, OnInit, Component, TemplateRef } from '@angular/core';
import { NgbDropdownModule, NgbNavModule, NgbTooltip, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { NgScrollbarModule } from 'ngx-scrollbar';
import { TemplateManagementService } from '../../../services/template-management.service';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatIcon } from '@angular/material/icon';
import { ToastrService } from 'ngx-toastr';
import { SpinnerService } from '../../../services/spinner.service';


@Component({
  selector: 'app-template-list',
  imports: [
    NgbNavModule,
    NgbDropdownModule,
    NgScrollbarModule,
    NgbTooltip,
    CommonModule,
    ReactiveFormsModule,
    MatIcon
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


    formGroup: FormGroup;
    courses: any[] = [];
    selectedCourseId: any | null = null;
    qpTemplateData: any = null;
    institutions: any [] = [];

    isUploadedCourseSyllabus = false; 
    uploadedFileNameCourseSyllabus = '';

    isUploadedExpertPreview = false;
    uploadedFileNameExpertPreview = ''

    isUploadedExpertQP = false; 
    uploadedFileNameExpertQP = '';

    isUploadedPrintPreviewQPDocument = false;
    uploadedFileNamePrintPreviewQP = ''

    isUploadedPrintPreviewQPAnswerDocument = false;
    uploadedFileNamePrintPreviewQPAnswer = ''
     

     

  constructor(private modalService: NgbModal,
              private templateService: TemplateManagementService,
              private fb: FormBuilder,
              private toasterService: ToastrService,
              private spinnerService: SpinnerService
  ) {

  }
  
    ngOnInit(): void {

      this.formGroup = this.fb.group({
        qpTemplateName: [''],
        degreeTypeName: [{ value: '', disabled: true }],
        regulationYear: [{ value: '', disabled: true }],
        batchYear: [{ value: '', disabled: true }],
        examYear: [{ value: '', disabled: true }],
        examMonth: [{ value: '', disabled: true }],
        examType:  [{ value: '', disabled: true }],
        semester:  [{ value: '', disabled: true }],
        institutionName: [{ value: '', disabled: true }],
        studentCount: [{ value: '', disabled: true }],
      });

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
        this.institutions = response.institutions;
        if(this.qpTemplateData)
        {
          this.formGroup.patchValue({
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
        }
        console.log("qpTemplate",JSON.stringify(this.qpTemplateData)  );
      });
    }

  onFileSelected(event: any,qpDocumentTypeId: number) {
    const file: File = event.target.files[0];
    if (file) {
      this.templateService.uploadDocument(file).subscribe({
        next: (response: any) => {
          console.log('Upload Success:', response);
          if (response?.documentId > 0) {
            let documentId = response?.documentId;
            if(qpDocumentTypeId == 1)
            {
              if (this.qpTemplateData?.documents) {
                const courseSyllabusDoc = this.qpTemplateData.documents.find(
                  (doc: { qpDocumentTypeId: number }) => doc.qpDocumentTypeId === 1
                );
  
                if (courseSyllabusDoc) {
                  courseSyllabusDoc.documentName = file.name;
                  //courseSyllabusDoc.documentUrl = response.documentUrl;
                  courseSyllabusDoc.documentId = documentId;
                }

                this.isUploadedCourseSyllabus = true; 
                this.uploadedFileNameCourseSyllabus = file.name;
              }
            }
            else if(qpDocumentTypeId == 2)
            {
              if (this.qpTemplateData?.documents) {
                const expertPreviewDoc = this.qpTemplateData.documents.find(
                  (doc: { qpDocumentTypeId: number }) => doc.qpDocumentTypeId === 2
                );
  
                if (expertPreviewDoc) {
                  expertPreviewDoc.documentName = file.name;
                  //expertPreviewDoc.documentUrl = response.documentUrl;
                  expertPreviewDoc.documentId = documentId;
                }

                this.isUploadedExpertPreview = true; 
                this.uploadedFileNameExpertPreview = file.name;
              }
            }
            else if(qpDocumentTypeId == 3)
              {
                if (this.qpTemplateData?.documents) {
                  const expertQpGenerationDoc = this.qpTemplateData.documents.find(
                    (doc: { qpDocumentTypeId: number }) => doc.qpDocumentTypeId === 3
                  );
    
                  if (expertQpGenerationDoc) {
                    expertQpGenerationDoc.documentName = file.name;
                   // expertQpGenerationDoc.documentUrl = response.documentUrl;
                    expertQpGenerationDoc.documentId = documentId;
                  }

                  this.isUploadedExpertQP = true; 
                  this.uploadedFileNameExpertQP = file.name;
                }
              }
              else if(qpDocumentTypeId == 6)
                {
                  if (this.qpTemplateData?.documents) {
                    const printPreviewQpDoc = this.qpTemplateData.userDocuments.find(
                      (doc: { qpDocumentTypeId: number }) => doc.qpDocumentTypeId === 6
                    );
      
                    if (printPreviewQpDoc) {
                      printPreviewQpDoc.documentName = file.name;
                      //printPreviewQpDoc.documentUrl = response.documentUrl;
                      printPreviewQpDoc.documentId = documentId;
                    }
  
                    this.isUploadedPrintPreviewQPDocument = true; 
                    this.uploadedFileNamePrintPreviewQP = file.name;
                  }
                }
                else if(qpDocumentTypeId == 7)
                  {
                    if (this.qpTemplateData?.documents) {
                      const printPreviewQPAnsDoc = this.qpTemplateData.userDocuments.find(
                        (doc: { qpDocumentTypeId: number }) => doc.qpDocumentTypeId === 7
                      );
        
                      if (printPreviewQPAnsDoc) {
                        printPreviewQPAnsDoc.documentName = file.name;
                       // printPreviewQPAnsDoc.documentUrl = response.documentUrl;
                        printPreviewQPAnsDoc.documentId = documentId;
                      }
    
                      this.isUploadedPrintPreviewQPAnswerDocument = true; 
                      this.uploadedFileNamePrintPreviewQPAnswer = file.name;
                    }
                  }
           
          }
        },
        error: (err) => console.error('Upload Error:', err)
      });
    }
  }


  removeFile(qpDocumentTypeId: number) {
  
    let removeFileName = this.getremoveFileName(qpDocumentTypeId);
    this.templateService.deleteDocument(removeFileName).subscribe({
      next: (response) => {
        console.log('Success:', response);
        if(response.message.includes('success'))
        {
          if(qpDocumentTypeId == 1)
          {
            this.uploadedFileNameCourseSyllabus = '';
            this.isUploadedCourseSyllabus = false; 
            const fileInput = document.getElementById('courseSyllabusDocument') as HTMLInputElement;
            if (fileInput) {
              fileInput.value = '';
            }
          }
          else if(qpDocumentTypeId == 2)
          {
            const fileInput = document.getElementById('expertPreviewDocument') as HTMLInputElement;
            if (fileInput) {
              fileInput.value = ''; 
            }
            this.uploadedFileNameExpertPreview = '';
            this.isUploadedExpertPreview = false; 
          }
          else if (qpDocumentTypeId == 3)
          {
            const fileInput = document.getElementById('expertQpGeneration') as HTMLInputElement;
            if (fileInput) {
              fileInput.value = ''; 
            }
            this.uploadedFileNameExpertQP = '';
            this.isUploadedExpertQP = false; 
          }
          else if (qpDocumentTypeId == 6)
            {
              const fileInput = document.getElementById('printPreviewQP') as HTMLInputElement;
              if (fileInput) {
                fileInput.value = ''; 
              }
              this.uploadedFileNamePrintPreviewQP = '';
              this.isUploadedPrintPreviewQPDocument = false; 
            }
            else if (qpDocumentTypeId == 7)
              {
                const fileInput = document.getElementById('printPreviewQPAnswer') as HTMLInputElement;
                if (fileInput) {
                  fileInput.value = ''; 
                }
                this.uploadedFileNamePrintPreviewQPAnswer = '';
                this.isUploadedPrintPreviewQPAnswerDocument = false; 
              }
         
        }
      },
      error: (error) => {
        console.error('Error deleting file:', error);
      }
    });
  }


  getremoveFileName(qpDocumentTypeId: number)
  {

    const fileNameMap: Record<number, string> = {
      1: this.uploadedFileNameCourseSyllabus,
      2: this.uploadedFileNameExpertPreview,
      3: this.uploadedFileNameExpertQP,
      6: this.uploadedFileNamePrintPreviewQP,
      7: this.uploadedFileNamePrintPreviewQPAnswer
    };
    
    return fileNameMap[qpDocumentTypeId] || '';
  }

  onSave() {
      
    if (this.formGroup.valid) {
    
      this.spinnerService.toggleSpinnerState(true);

      const formData = this.formGroup.value;
      this.qpTemplateData.qpTemplateName = formData.qpTemplateName;
      this.templateService.saveQpTemplate(this.qpTemplateData).subscribe({
        next: (response) => {
          console.log('Save successful:', response);
          this.spinnerService.toggleSpinnerState(false);
          this.toasterService.success('Qp Template added successfully')
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

    //#endregion
    
}

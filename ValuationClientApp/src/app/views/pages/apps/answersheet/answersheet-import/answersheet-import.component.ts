import {
  AfterViewInit,
  OnInit,
  Component,
  TemplateRef,
  ViewChild,
} from '@angular/core';
import {
  NgbDropdownModule,
  NgbNavModule,
  NgbTooltip,
  NgbModal,
  NgbModalRef,
} from '@ng-bootstrap/ng-bootstrap';
import { NgScrollbarModule } from 'ngx-scrollbar';
import { CommonModule } from '@angular/common';
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
import { Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { AnswersheetService } from '../../../services/answersheet.service';
import { AnswersheetImportService } from '../../../services/answersheet-import.service';
import { InstituteService } from '../../../services/institute.service';
import { SpinnerService } from '../../../services/spinner.service';

@Component({
  selector: 'app-answersheet-import',
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
  ],
  templateUrl: './answersheet-import.component.html',
  styleUrl: './answersheet-import.component.scss',
})
//
export class AnswersheetImportComponent implements OnInit {
  ///
  dataSourceInstitutes: any[] = [];
  dataSourceExamYears: any[] = [];
  dataSourceExamMonths: any[] = [];
  dataSourceExaminations: any[] = [];
  dataSourceAnswerSheetImports: any[] = [];
  dataSourceAnswerSheetImportDetails: any[] = [];

  selectedInstituteId: number = 0;
  selectedExamMonth: string = '0';
  selectedExamYear: string = '0';
  selectedExaminationId: number = 0;
  selectedanswersheetImportId: number = 0;
  selectedAnswersheetName: string = '';

  isDummyNumberImported = false;

  dummyNumberImportForm = new FormGroup({
    name: new FormControl('', [Validators.required, Validators.minLength(3)]),
    file: new FormControl([], [Validators.required]),
    fileSource: new FormControl([], [Validators.required]),
  });

  displayedColumns: string[] = ['dummyNumber', 'isValid', 'errorMessage'];

  modalRef: NgbModalRef;

  @ViewChild('confirmModule') confirmModule: any;

  constructor(
    private fb: FormBuilder,
    private modalService: NgbModal,
    private answersheetService: AnswersheetService,
    private instituteService: InstituteService,
    private answersheetImportService: AnswersheetImportService,
    private toasterService: ToastrService,
    private spinnerService: SpinnerService
  ) {}

  ngOnInit(): void {
    this.loadAllInstitues();
    this.loadAllExamYears();
    this.loadAllExamMonths();

    this.dataSourceExaminations = [];
    this.dataSourceAnswerSheetImports = [];
    this.dataSourceAnswerSheetImportDetails = [];
    this.selectedAnswersheetName = '';
  }

  loadAllInstitues(): void {
    this.instituteService.getInstitutions().subscribe({
      next: (data) => {
        this.dataSourceInstitutes = data;
        console.log('Institutes loaded:', this.dataSourceInstitutes);
      },
      error: (err) => {
        console.error('Error loading Institutes:', err);
      },
    });
  }

  loadAllExamYears(): void {
    this.answersheetService.getExamYears().subscribe({
      next: (data) => {
        this.dataSourceExamYears = data;
        console.log('ExamYears loaded:', this.dataSourceExamYears);
      },
      error: (err) => {
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

  loadExaminationItems(): void {
    this.selectedExaminationId = 0;
    if (
      this.selectedInstituteId !== 0 &&
      this.selectedExamYear !== '0' &&
      this.selectedExamMonth !== '0'
    ) {
      this.answersheetImportService
        .GetExaminationItems(
          this.selectedInstituteId,
          this.selectedExamYear,
          this.selectedExamMonth
        )
        .subscribe({
          next: (data: any) => {
            this.dataSourceExaminations = data;
            console.log(
              'dataSourceExaminations  loaded:',
              this.dataSourceExaminations
            );
          },
          error: (err: any) => {
            console.error('Error loading dataSourceExaminations:', err);
          },
        });
    } else {
      this.dataSourceExaminations = [];
    }
  }

  loadAnswersheetImports() {
    //----------
    this.dataSourceAnswerSheetImports = [];
    this.dataSourceAnswerSheetImportDetails = [];
    this.selectedAnswersheetName = '';
    //----------
    this.answersheetImportService
      .GetAnswersheetImports(this.selectedExaminationId)
      .subscribe({
        next: (data: any) => {
          this.dataSourceAnswerSheetImports = data;
          console.log(
            'AnswerSheetImports loaded:',
            this.dataSourceAnswerSheetImports
          );
        },
        error: (err: any) => {
          this.dataSourceAnswerSheetImports = [];
          console.error('Error loading ExamMonths:', err);
        },
      });
  }

  loadAnswersheetImportDetails(
    answerSheetImportId: number,
    documentName: string
  ) {
    this.selectedanswersheetImportId = answerSheetImportId;
    this.selectedAnswersheetName = documentName;
    this.dataSourceAnswerSheetImportDetails = [];
    this.answersheetImportService
      .GetAnswersheetImportDetails(answerSheetImportId)
      .subscribe({
        next: (data: any) => {
          this.dataSourceAnswerSheetImportDetails = data;
          console.log(
            'dataSourceAnswerSheetImportDetails loaded:',
            this.dataSourceAnswerSheetImportDetails
          );
        },
        error: (err: any) => {
          this.dataSourceAnswerSheetImportDetails = [];
          console.error('Error-GetAnswersheetImportDetails:', err);
        },
      });
  }

  private getValue(event: Event) {
    return (event.target as HTMLSelectElement).value;
  }

  onInstituteChange(event: Event): void {
    this.selectedInstituteId = parseInt(this.getValue(event));
    this.loadExaminationItems();
  }

  onExamYearChange(event: Event): void {
    this.selectedExamYear = this.getValue(event);
    this.loadExaminationItems();
  }

  onExamMonthChange(event: Event): void {
    this.selectedExamMonth = this.getValue(event);
    this.loadExaminationItems();
  }

  onExaminationChange(event: Event): void {
    this.selectedExaminationId = parseInt(this.getValue(event));
    this.isDummyNumberImported = false;
    this.loadAnswersheetImports();
    console.log('this.selectedExaminationId ' + this.selectedExaminationId);
  }

  onDummyNumberFileChange(event: any) {
    if (event.target.files.length > 0) {
      this.isDummyNumberImported = true;
      const file = event.target.files[0];
      this.dummyNumberImportForm.patchValue({
        fileSource: file,
      });
    }
  }

  resetImportFormControls() {
    this.dummyNumberImportForm.controls['file'].setValue([]);
    this.isDummyNumberImported = false;
  }

  get gedDummyNumberformControls() {
    return this.dummyNumberImportForm.controls;
  }

  submitDummyNumberImport() {
    this.spinnerService.toggleSpinnerState(true);
    console.log("submit")
    const formData = new FormData();
    const fileSource: any = this.dummyNumberImportForm.get('fileSource')?.value;
    if (fileSource !== null && fileSource !== undefined) {
      formData.append('file', fileSource);
    }
    console.log(formData)
    console.log(this.selectedExaminationId)
    this.answersheetImportService
      .importAnswerSheetDummyNumbers(formData, this.selectedExaminationId)
      .subscribe({
        next: (response) => {
          this.spinnerService.toggleSpinnerState(false);
          this.toasterService.success('Imported successfully.');
          this.resetImportFormControls();
          this.loadAnswersheetImports();
        },
        error: (errorResponse) => {
          this.spinnerService.toggleSpinnerState(false);
          this.toasterService.error('Error on Import process.');
          this.resetImportFormControls();
          this.loadAnswersheetImports();
        },
        complete: () => {},
      });
  }

  promptBeforeReviewCompletion() {
    this.modalRef = this.modalService.open(this.confirmModule, {
      size: 'md',
      backdrop: 'static',
    });
  }

  completeDummyNumberReview() {
    this.modalRef.close();
    // if (this.obtainedMarks == 0) {
    //   this.toastr.warning('Obained Marks is 0. Please evaluate.');
    // } else {
    //   this.evaluationService
    //     .completeEvaluation(this.primaryData.answersheetId, this.loggedinUserId)
    //     .subscribe(
    //       (data: any) => {
    //         console.log('respo', data);
    //         if (data.message.toLowerCase() == 'success') {
    //           this.toastr.success('Evaluation completed successfully');
    //           this.backToList();
    //         } else {
    //           console.error('Failed to complete evaluation');
    //           this.toastr.error('Failed to complete evaluation.');
    //         }
    //       },
    //       (error) => {
    //         console.error('Error while saving evaluation:', error);
    //         this.toastr.error('Failed to complete evaluation.');
    //       }
    //     );
    // }
  }

  ////
} // end of class

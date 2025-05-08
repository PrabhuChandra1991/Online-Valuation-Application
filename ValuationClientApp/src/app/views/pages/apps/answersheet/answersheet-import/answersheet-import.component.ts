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
  dataSourceExamCourses: any[] = [];

  dataSourceAnswerSheetImports: any[] = [];
  dataSourceAnswerSheetImportDetails: any[] = [];

  selectedInstituteId: number = 0;
  selectedExamMonth: string = '0';
  selectedExamYear: string = '0';
  selectedCourseId: number = 0;

  selectedAnswersheetImportId: number = 0;
  selectedAnswersheetImportName: string = '';
  selectedAnswersheetImportIsReviewCompleted: boolean = false;

  isDummyNumberImported = false;

  dummyNumberImportForm = new FormGroup({
    name: new FormControl('', [Validators.required, Validators.minLength(3)]),
    file: new FormControl([], [Validators.required]),
    fileSource: new FormControl([], [Validators.required]),
  });

  displayedColumns: string[] = ['dummyNumber', 'isValid', 'errorMessage'];

  modalRef: NgbModalRef;

  @ViewChild('confirmModule') confirmModule: any;
  @ViewChild('deleteconfirmModule') deleteconfirmModule: any;

  constructor(
    private fb: FormBuilder,
    private modalService: NgbModal,
    private answersheetService: AnswersheetService,
    private instituteService: InstituteService,
    private answersheetImportService: AnswersheetImportService,
    private toasterService: ToastrService,
    private spinnerService: SpinnerService
  ) { }

  ngOnInit(): void {
    this.loadAllInstitues();
    this.loadAllExamYears();
    this.loadAllExamMonths();

    this.dataSourceExamCourses = [];
    this.dataSourceAnswerSheetImports = [];
    this.dataSourceAnswerSheetImportDetails = [];
    this.selectedAnswersheetImportName = '';
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
    this.selectedCourseId = 0;
    if (
      this.selectedInstituteId !== 0 &&
      this.selectedExamYear !== '0' &&
      this.selectedExamMonth !== '0'
    ) {
      this.answersheetImportService
        .GetExaminationCourseItems(
          this.selectedInstituteId,
          this.selectedExamYear,
          this.selectedExamMonth
        )
        .subscribe({
          next: (data: any) => {
            this.dataSourceExamCourses = data;
            console.log(
              'dataSourceExaminations  loaded:',
              this.dataSourceExamCourses
            );
          },
          error: (err: any) => {
            console.error('Error loading dataSourceExaminations:', err);
          },
        });
    } else {
      this.dataSourceExamCourses = [];
    }
  }

  loadAnswersheetImports() {
    //----------
    this.dataSourceAnswerSheetImports = [];
    this.dataSourceAnswerSheetImportDetails = [];
    this.selectedAnswersheetImportName = '';
    //----------
    this.answersheetImportService
      .GetAnswersheetImports(this.selectedInstituteId, this.selectedExamYear, this.selectedExamMonth, this.selectedCourseId)
      .subscribe({
        next: (data: any) => {
          this.dataSourceAnswerSheetImports = data;
        },
        error: (err: any) => {
          this.dataSourceAnswerSheetImports = [];
        },
      });
  }

  loadAnswersheetImportDetails(
    answerSheetImportId: number,
    documentName: string,
    isReviewCompleted: boolean
  ) {
    this.selectedAnswersheetImportId = answerSheetImportId;
    this.selectedAnswersheetImportName = documentName;
    this.selectedAnswersheetImportIsReviewCompleted = isReviewCompleted;
    this.dataSourceAnswerSheetImportDetails = [];
    this.answersheetImportService
      .GetAnswersheetImportDetails(answerSheetImportId)
      .subscribe({
        next: (data: any) => {
          this.dataSourceAnswerSheetImportDetails = data;
        },
        error: (err: any) => {
          this.dataSourceAnswerSheetImportDetails = [];
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

  onExamCourseChange(event: Event): void {
    this.selectedCourseId = parseInt(this.getValue(event));
    this.isDummyNumberImported = false;
    this.selectedAnswersheetImportId = 0;
    this.selectedAnswersheetImportIsReviewCompleted = false;
    this.selectedAnswersheetImportName = "";
    this.loadAnswersheetImports();
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
    const formData = new FormData();
    const fileSource: any = this.dummyNumberImportForm.get('fileSource')?.value;

    if (fileSource !== null && fileSource !== undefined) {
      formData.append('file', fileSource);
    }

    this.answersheetImportService
      .importAnswerSheetDummyNumbers(
        formData,
        this.selectedInstituteId,
        this.selectedExamYear,
        this.selectedExamMonth,
        this.selectedCourseId)
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
        complete: () => { },
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
    this.answersheetImportService
      .ReviewCompletedAndApproving(this.selectedAnswersheetImportId)
      .subscribe(
        (data: any) => {
          this.toasterService.success('Review completed successfully.');
          this.loadAnswersheetImports();
          this.selectedAnswersheetImportIsReviewCompleted = true;
          this.loadAnswersheetImportDetails(this.selectedAnswersheetImportId, this.selectedAnswersheetImportName, this.selectedAnswersheetImportIsReviewCompleted);
        },
        (error) => {
          if (error.error.message == "UNIQUE-KEY-VIOLATION")
            this.toasterService.error('Some Dummy Numbers are already exists in database..Please check..');
          else
            this.toasterService.error('Failed to complete review.');
        }
      );
  }

  promptBeforeDeleteImportedFile() {
    this.modalRef = this.modalService.open(this.deleteconfirmModule, {
      size: 'md',
      backdrop: 'static',
    });
  }

  deleteAnswersheetImportedData() {
    this.modalRef.close();
    this.answersheetImportService.DeleteAnswersheetImportedData(this.selectedAnswersheetImportId)
      .subscribe({
        next: (data: any) => {
          this.toasterService.success('File deleted successfully.');
          this.loadAnswersheetImports();
          this.selectedAnswersheetImportId = 0;
          this.selectedAnswersheetImportIsReviewCompleted = false;
          this.selectedAnswersheetImportName = "";
          this.dataSourceAnswerSheetImportDetails = [];
        },
        error: (err) => {
          this.toasterService.error('File delete process failed.');
        }
      });
  }

  ////
} // end of class

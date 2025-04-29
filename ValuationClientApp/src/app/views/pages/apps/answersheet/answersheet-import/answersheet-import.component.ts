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

  selectedInstituteId: number = 0;
  selectedExamMonth: string = '';
  selectedExamYear: string = '';
  selectedExaminationId: number = 0;

  isDummyNumberImported = false;

  dummyNumberImportForm = new FormGroup({
    name: new FormControl('', [Validators.required, Validators.minLength(3)]),
    file: new FormControl([], [Validators.required]),
    fileSource: new FormControl([], [Validators.required]),
  });

  constructor(
    private fb: FormBuilder,
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
    this.resetExaminationAndForFile();

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

  resetExaminationAndForFile() {
    this.selectedExaminationId = 0;
    this.dummyNumberImportForm.controls['file'].setValue([]);
    this.isDummyNumberImported = false;
  }

  get gedDummyNumberformControls() {
    return this.dummyNumberImportForm.controls;
  }

  submitDummyNumberImport() {
    // this.spinnerService.toggleSpinnerState(true);
    const formData = new FormData();
    const fileSource: any = this.dummyNumberImportForm.get('fileSource')?.value;
    if (fileSource !== null && fileSource !== undefined) {
      formData.append('file', fileSource);
    }
    this.answersheetImportService
      .importAnswerSheetDummyNumbers(formData, this.selectedExaminationId)
      .subscribe({
        next: (response) => {
          this.toasterService.success('Imported successfully.');
          this.resetExaminationAndForFile();
        },
        error: (errorResponse) => {
          //this.toastr.error('Failed to import data. Please try again.');
          //this.spinnerService.toggleSpinnerState(false);
        },
        complete: () => {
          //this.spinnerService.toggleSpinnerState(false);
        },
      });
  }

  ////
} // end of class

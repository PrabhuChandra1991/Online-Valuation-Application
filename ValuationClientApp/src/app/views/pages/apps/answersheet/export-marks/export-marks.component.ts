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
import { InstituteService } from '../../../services/institute.service';
import { TemplateManagementService } from '../../../services/template-management.service';
import { ThirdPartyDraggable } from '@fullcalendar/interaction/index.js';
import { SpinnerService } from '../../../services/spinner.service';

@Component({
  selector: 'app-export-marks',
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
  templateUrl: './export-marks.component.html',
  styleUrl: './export-marks.component.scss'
})
export class ExportMarksComponent {

  dataSourceInstitutes: any[] = [];
  dataSourceExamYears: any[] = [];
  dataSourceExamMonths: any[] = [];
  dataSourceExamTypes: any[] = [];
  dataSourceCourses: any[] = [];
  selectedExamYear: string = "";
  selectedExamMonth: string = "";
  selectedExamType: string = "";
  selectedInstituteId: number = 0;
  selectedCourseId: number = 0;

  courseControl = new FormControl('');
  courseList: string[] = [];
  filteredOptions: string[] = [];

  constructor(
    private answersheetService: AnswersheetService,
    private instituteService: InstituteService,
    private toasterService: ToastrService,
    private spinnerService: SpinnerService,
  ) { }

  ngOnInit(): void {
    this.loadAllInstitues();
    this.loadAllCourses();
    this.loadExamYears();
    this.loadExamMonths();
    this.loadAllExamTypes();
  }


  loadAllInstitues(): void {
    this.instituteService.getInstitutions().subscribe({
      next: (data) => {
        this.dataSourceInstitutes = data;
      },
      error: (error) => {
        console.error('Error loading Institutes:', error);
      },
    });
  }

  loadExamYears(): void {
    this.answersheetService.getExamYears().subscribe({
      next: (data) => {
        this.dataSourceExamYears = data;
      },
      error: (err) => {
        console.error('Error loading ExamYears:', err);
      },
    });
  }

  loadExamMonths(): void {
    this.answersheetService.getExamMonths().subscribe({
      next: (data: any) => {
        this.dataSourceExamMonths = data;
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
      },
      error: (err: any) => {
        console.error('Error loading ExamTypes:', err);
      },
    });
  }

  loadAllCourses(): void {
    if (this.selectedExamYear != "" && this.selectedExamMonth != "" && this.selectedExamType != "") {
      this.answersheetService
        .getCoursesHavingAnswersheetForExportMark
        (this.selectedExamYear, this.selectedExamMonth, this.selectedExamType, this.selectedInstituteId).subscribe({
          next: (data) => {
            this.dataSourceCourses = data;

            console.log('Course:', data);

            for (const course of data) {
              this.courseList.push(course.code + " - " + course.name + " - (" + course.count + ")");
            }
    
            console.log('Course loaded:', this.courseList);
    
            this.courseControl.valueChanges.subscribe(value => {
              const filterValue = value?.toLowerCase() || '';
              this.filteredOptions = this.courseList.filter(option =>
                option.toLowerCase().includes(filterValue)
              );
            });

          },
          error: (error) => {
            console.error('Error loading Course:', error);
          },
        });
    }
  }

  onInstituteChange(event: Event): void {
    this.selectedInstituteId = parseInt((event.target as HTMLSelectElement).value);
    this.loadAllCourses();
  }

  onExamYearChange(event: Event): void {
    this.selectedExamYear = (event.target as HTMLSelectElement).value;
    this.loadAllCourses();
  }

  onExamMonthChange(event: Event): void {
    this.selectedExamMonth = (event.target as HTMLSelectElement).value;
    this.loadAllCourses();
  }

  onExamTypeChange(event: Event): void {
    this.selectedExamType = (event.target as HTMLSelectElement).value;
    this.loadAllCourses();
  }

  showList() {
    this.filteredOptions = this.courseList;
    document.querySelector('.course')?.classList.remove('hide');
  }

  hideList() {
    setTimeout(() => {
      this.filteredOptions = [];
      document.querySelector('.course')?.classList.add('hide');
    }, 200);
  }

  onCourseChange(option: string) {    
    this.courseControl.setValue(option);
    this.filteredOptions = []; // hide dropdown after selection

    let index = option.indexOf('-');
    let code = option.substring(0, index).trim();
    let result = this.dataSourceCourses.filter(course => course.code === code);
    this.selectedCourseId = result[0].courseId;

    console.log("Course selected: ", result)
  }

  exportMarks(): void {
    this.spinnerService.toggleSpinnerState(true);
    this.answersheetService.exportMarks(
      this.selectedInstituteId, this.selectedCourseId,
      this.selectedExamYear, this.selectedExamMonth, this.selectedExamType).subscribe({
        next: (response: any) => {
          this.openFileInTab(response.base64Content, response.fileName, response.contentType);

          this.toasterService.success('Data submitted successfully!');
          this.spinnerService.toggleSpinnerState(false);
        },
        error: (err: any) => {
          if (err.error.message === "EVALUATION-NOT-COMPLETED")
            this.toasterService.error('Evaluation is not completed for all answersheets. Please check.');
          else
            this.toasterService.error('Error exporting marks. Please try again.');
          this.spinnerService.toggleSpinnerState(false);
        },
      });
  }

  openFileInTab(base64String: string, filename: string, contentType: string) {
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
  }
}

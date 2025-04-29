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
institutes: any[] = [];
  courses: any[] = [];
  examYears: any[] = [];
  examMonths: any[] = [];
  selectedExamYear: string = "";
  selectedExamMonth: string = "";
  selectedInstituteId: number = 0;
  selectedCourseId: number = 0;
 
  constructor(
    private answersheetService: AnswersheetService,
    private instituteService: InstituteService,
    private toasterService: ToastrService,
    private spinnerService: SpinnerService,
  ) {}

  ngOnInit(): void {
    this.loadAllInstitues();
    this.loadAllCourses();
    this.loadExamYears();
    this.loadExamMonths();
  }
  loadExamYears(): void {
    this.examYears = ['2025','2026','2027','2028','2029','2030','2031','2032','2033','2034','2035'];
  }
  loadExamMonths(): void {
    this.examMonths = ['APRIL/MAY', 'NOV/DEC'];
  }

  loadAllInstitues(): void {
    this.instituteService.getInstitutions().subscribe({
      next: (data) => {
        this.institutes = data;
        console.log('Institutes loaded:', this.institutes);
      },
      error: (error) => {
        console.error('Error loading Institutes:', error);
      },
    });
  }

  loadAllCourses(): void {
    this.answersheetService.getCourses().subscribe({
      next: (data) => {
        this.courses = data;
        console.log('Course loaded:', this.courses);
      },
      error: (error) => {
        console.error('Error loading Course:', error);
      },
    });
  }
  onInstituteChange(event: Event): void {
    this.selectedInstituteId =parseInt( (event.target as HTMLSelectElement).value);
  }
  onCourseChange(event: Event): void {
    this.selectedCourseId = parseInt((event.target as HTMLSelectElement).value);
  }
  onExamYearChange(event: Event): void {
    this.selectedExamYear = (event.target as HTMLSelectElement).value;
  }
  onExamMonthChange(event: Event): void {
    this.selectedExamMonth = (event.target as HTMLSelectElement).value;
  }
  exportMarks(): void {
    this.spinnerService.toggleSpinnerState(true);
      this.answersheetService.exportMarks(this.selectedInstituteId, this.selectedCourseId,this.selectedExamYear,this.selectedExamMonth).subscribe({
        next: (response:any) => {
          this.openFileInTab(response.base64Content, response.fileName, response.contentType);
          
          this.toasterService.success('Data submitted successfully!');
          this.spinnerService.toggleSpinnerState(false);
        },
        error: (error:any) => {
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

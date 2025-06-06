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
import { InstituteService } from '../../../services/institute.service';
import { encode } from 'js-base64';

@Component({
  selector: 'app-answersheet-management',
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
  templateUrl: './answersheet-management.component.html',
  styleUrl: './answersheet-management.component.scss',
})
export class AnswersheetManagementComponent {
  answersheets: any[] = [];

  displayedColumns: string[] = [
    'dummyNumber',
    'allocatedUserName',
    'isEvaluateCompleted',
    'totalObtainedMark',
    'revertEvaluation',
    'actions'
  ];

  @ViewChild(MatPaginator) paginator: MatPaginator;
  @ViewChild(MatSort) sort: MatSort;

  ddlYears = new FormControl('');
  ddlMonths = new FormControl('');
  ddlTypes = new FormControl('');
  ddlCourses = new FormControl('');

  dataSource = new MatTableDataSource<any>([]);

  dataSourceExamYears: any[] = [];
  dataSourceExamMonths: any[] = [];
  dataSourceExamTypes: any[] = [];

  institutes: any[] = [];
  courses: any[] = [];

  selectedExamYear: string = '';
  selectedExamMonth: string = '';
  selectedExamType: string = '';
  selectedCourseId: number = 0;

  constructor(
    private fb: FormBuilder,
    private answersheetService: AnswersheetService,
    private instituteService: InstituteService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.loadAllExamYears();
    this.loadAllExamMonths();
    this.loadAllExamTypes();
    this.ddlYears.setValue('0');
    this.ddlMonths.setValue('0');
    this.ddlTypes.setValue('0');
    this.courses = [];
    this.ddlCourses.setValue('0');
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
  }

  loadCourses(): void {
    this.answersheetService.getCoursesWithAnswersheet(this.selectedExamYear, this.selectedExamMonth, this.selectedExamType).subscribe({
      next: (data) => {
        this.courses = data;
        console.log('Course loaded:', this.courses);
      },
      error: (error) => {
        console.error('Error loading Course:', error);
      },
    });
    this.ddlCourses.setValue('0');
  }

  applyFilter(event: Event) {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();

    if (this.dataSource.paginator) {
      this.dataSource.paginator.firstPage();
    }
  }

  onExamYearChange(event: Event): void {
    this.selectedExamYear = (event.target as HTMLSelectElement).value;
    this.ddlMonths.setValue('0');
    this.ddlTypes.setValue('0');
    this.courses = [];
    //this.ddlCourses.setValue('0');
    this.dataSource.data = [];
  }

  onExamMonthChange(event: Event): void {
    this.selectedExamMonth = (event.target as HTMLSelectElement).value;
    this.courses = [];
    //this.ddlCourses.setValue('0');
    this.answersheets = [];
  }

  onExamTypeChange(event: Event): void {
    this.selectedExamType = (event.target as HTMLSelectElement).value;
    this.loadCourses();
    //this.ddlCourses.setValue('0');
    this.answersheets = [];
  }

  onCourseChange(event: Event): void {
    this.selectedCourseId = parseInt((event.target as HTMLSelectElement).value);
    this.loadData();
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

  loadData(): void {
    this.loadAnswersheets(this.selectedCourseId);
  }

  loadAnswersheets(courseId: number): void {
    this.answersheetService
      .getAnswersheetDetails(this.selectedExamYear, this.selectedExamMonth, this.selectedExamType, courseId)
      .subscribe({
        next: (data: any[]) => {
          this.answersheets = data;
          this.dataSource.data = this.answersheets;
          this.dataSource.paginator = this.paginator;
          this.dataSource.sort = this.sort;
          // console.log('history data:', this.answersheets);
        },
        error: (errRes: any) => {
          console.error('Error loading history:', errRes);
        },
      });
  }


  revertEvaluation(dummyNumber: any) {
    if (this.selectedCourseId) {
      let answersheet = this.answersheets.filter(m => m.dummyNumber == dummyNumber);
      let answersheetId = answersheet[0].answersheetId;
      this.answersheetService.revertEvaluation(answersheetId).subscribe({
        next: (data) => {
          this.loadData();
        }
      });
    }
  }

  edit(dummyNumber: any) {
    if (this.selectedCourseId) {
      let answersheet = this.answersheets.filter(m => m.dummyNumber == dummyNumber);
      let answersheetId = answersheet[0].answersheetId;
      this.router.navigate(['/apps/evaluate', encode(String(answersheetId))]);
    }
  }

  viewEvaluation(answerSheetId: any) {
    this.router.navigate(['/apps/viewevaluation/' + encode(String(answerSheetId))]);
    // const features = `
    //   fullscreen=yes,
    //   toolbar=yes,
    //   location=no,
    //   status=no,
    //   menubar=no,
    //   scrollbars=no,
    //   resizable=no,
    //   top=0,
    //   left=0,
    //   width=${screen.width},
    //   height=${screen.height}
    // `;
    // const win = window.open(url, '_blank', features);
    // win?.focus();
  }

  //---------
  //---------
}

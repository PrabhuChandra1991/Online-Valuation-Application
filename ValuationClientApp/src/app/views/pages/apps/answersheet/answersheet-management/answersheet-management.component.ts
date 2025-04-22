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
    'institutionName',
    'regulationYear',
    'batchYear',
    'degreeTypeName',
    'examType',
    'semester',
    'courseCode',
    'examMonth',
    'examYear',
    'dummyNumber',
    'allocatedUserName'
  ];

  @ViewChild(MatPaginator) paginator: MatPaginator;
  @ViewChild(MatSort) sort: MatSort;

  dataSource = new MatTableDataSource<any>([]);

  institutes: any[] = [];
  courses: any[] = [];

  selectedInstituteId: number = 0;
  selectedCourseId: number = 0;

  constructor(
    private fb: FormBuilder,
    private answersheetService: AnswersheetService,
    private instituteService: InstituteService
  ) {}

  ngOnInit(): void {
    this.loadAllInstitues();
    this.loadAllCourses();
  }

  applyFilter(event: Event) {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();

    if (this.dataSource.paginator) {
      this.dataSource.paginator.firstPage();
    }
  }

  loadAnswersheets(institutionId: number, courseId: number): void {
    this.answersheetService
      .getAnswersheetDetails(institutionId, courseId)
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

  onInstituteChange(event: Event): void {
    this.selectedInstituteId =parseInt( (event.target as HTMLSelectElement).value);
    this.loadData();
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

  loadData(): void {
    this.loadAnswersheets(this.selectedInstituteId, this.selectedCourseId);
  }

  //---------
  //---------
}

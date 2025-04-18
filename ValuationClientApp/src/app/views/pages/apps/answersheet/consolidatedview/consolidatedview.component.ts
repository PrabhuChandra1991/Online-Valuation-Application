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
  selector: 'app-consolidatedview',
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
  templateUrl: './consolidatedview.component.html',
  styleUrl: './consolidatedview.component.scss'
})
export class ConsolidatedviewComponent {
  importHistoryForm!: FormGroup;

  gridDataItems: any[] = [];

  displayedColumns: string[] = [
    'institutionCode',
    'regulationYear',
    'batchYear',
    'degreeType',
    'examType',
    'semester',
    'courseCode',
    'examMonth',
    'examYear', 
    'studentTotalCount',
    'answerSheetTotalCount',
    'answerSheetAllocatedCount',
    'answerSheetNotAllocatedCount',
    'actions'
  ];

  @ViewChild(MatPaginator) paginator: MatPaginator;
  @ViewChild(MatSort) sort: MatSort;

  dataSource = new MatTableDataSource<any>([]);

  institutes: any[] = [];
   
  selectedInstituteId: string = ''; 

  constructor(
    private fb: FormBuilder,
    private answersheetService: AnswersheetService,
    private instituteService: InstituteService
  ) {}

  ngOnInit(): void {
    this.loadAllInstitues(); 
  }

  applyFilter(event: Event) {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();

    if (this.dataSource.paginator) {
      this.dataSource.paginator.firstPage();
    }
  }

  loadGridData(): void {
    this.answersheetService
      .getConsolidatedExamAnswersheets( parseInt( this.selectedInstituteId))  
      .subscribe({
        next: (data: any[]) => {
          this.gridDataItems = data;
          this.dataSource.data = this.gridDataItems;
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
    this.selectedInstituteId = (event.target as HTMLSelectElement).value;
    this.loadGridData();
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
  
}

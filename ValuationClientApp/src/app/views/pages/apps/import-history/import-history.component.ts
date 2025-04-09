import { AfterViewInit, OnInit, Component, TemplateRef, ViewChild} from '@angular/core';
import { NgbDropdownModule, NgbNavModule, NgbTooltip, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { NgScrollbarModule } from 'ngx-scrollbar';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, Validators, FormsModule, ReactiveFormsModule, FormControl } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import {MatCheckboxModule} from '@angular/material/checkbox';
import { ImportHistoryService } from '../../services/import-history.service';


@Component({
  selector: 'app-import-history',
  imports: [NgbNavModule,
    NgbDropdownModule,
    NgScrollbarModule,
    NgbTooltip,
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatTableModule, MatSortModule, MatPaginatorModule,
    MatCheckboxModule
  ],
  templateUrl: './import-history.component.html',
  styleUrl: './import-history.component.scss'
})
export class ImportHistoryComponent implements OnInit{

  importHistoryForm!: FormGroup;

  importHistories: any[] = [];

  displayedColumns: string[] = ['documentId', 'documentName','documentUrl' ,'importedBy','totalRecordsImported','institutionsCount','totalCourseCount'
                              ,'departmentsCount'];

  dataSource = new MatTableDataSource<any>([]);

  @ViewChild(MatPaginator) paginator: MatPaginator;
  @ViewChild(MatSort) sort: MatSort;

  constructor(private fb: FormBuilder,
              private importHistoryService:ImportHistoryService,
              private router: Router
              ){}

  ngOnInit(): void {

    this.loadImportHistory();

    // this.importHistoryForm = this.fb.group({
    //   documentId:[''],
    //   documentName:[''],
    //   documentUrl:[''],
    //   importedBy:[''],
    //   totalRecordsImported:[''],
    //   totalCourseCount:[''],
    //   institutionsCount:[''],
    //   departmentsCount:['']
    // });
  }

  applyFilter(event: Event) {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();

    if (this.dataSource.paginator) {
      this.dataSource.paginator.firstPage();
    }
  }

  loadImportHistory(): void {
    this.importHistoryService.getImportHistory().subscribe({
      next: (data: any[]) => {
        this.importHistories = data;
        this.dataSource.data = this.importHistories;
        this.dataSource.paginator = this.paginator;
       this.dataSource.sort = this.sort;
       console.log('history data:', this.importHistories);
      },

      error: (error) => {
        console.error('Error loading history:', error);
      }
    });
  }

  gotoImportMaster() {
    this.router.navigate(['/apps/master']);
  }

}

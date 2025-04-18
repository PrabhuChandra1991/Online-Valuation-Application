import { NgClass, NgFor } from '@angular/common';
import { OnInit, Component, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PdfViewerModule } from 'ng2-pdf-viewer';
import { ToastrService } from 'ngx-toastr';
import { Router } from '@angular/router';
import { EvaluationService } from '../../../services/evaluation.service';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { encode } from 'js-base64';

@Component({
  selector: 'app-evaluate',
  imports: [NgClass,
    NgFor,
    CommonModule,
    PdfViewerModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,],
  templateUrl: './evaluation-list.component.html',
  styleUrl: './evaluation-list.component.scss'
})

export class EvaluationListComponent implements OnInit {
  answerSheetList: any[] = [];
  displayedColumns: string[] = ['institutionName', 'courseCode', 'courseName', 'degreeTypeName', 'regulationYear', 'batchYear', 'examYear', 'examMonth', 'semester', 'actions'];
  dataSource = new MatTableDataSource<any>([]);

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  constructor(
    private toastr: ToastrService,
    private evaluationService: EvaluationService,
    private router: Router
  ) { }

  ngOnInit(): void {
    console.log("ngOnInit.................");
    const loggedInUser = localStorage.getItem('userData') || "";
    if (loggedInUser) {
      //console.log("userData: ", JSON.parse(loggedInUser));
      const userData = JSON.parse(loggedInUser);
      this.loadPrimaryDetails(userData.userId);
    }
  }

  loadPrimaryDetails(userId: number) {
    this.evaluationService.getAnswerSheetDetails(userId).subscribe(
      (data: any[]) => {
        console.log('API Data:', data);
        this.answerSheetList = data;
        this.dataSource.data = this.answerSheetList; // Avoid reassigning MatTableDataSource
        this.dataSource.paginator = this.paginator;
        this.dataSource.sort = this.sort;
      },
      (error) => {
        console.error('Error fetching primary data:', error);
        this.toastr.error('Failed to load primary data.');
      }
    );
  }

  applyFilter(event: Event) {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();
  }

  evaluate(answerSheetId: number) {
    let primaryData = this.answerSheetList.filter(x => x.answersheetId == answerSheetId)[0];
    this.router.navigate(['/apps/evaluate', encode(JSON.stringify(primaryData))]);
  }

}

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
import { MatRadioModule } from '@angular/material/radio';

@Component({
  selector: 'app-evaluate',
  imports: [NgClass,
    NgFor,
    CommonModule,
    PdfViewerModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatRadioModule],
  templateUrl: './evaluation-list.component.html',
  styleUrl: './evaluation-list.component.scss'
})

export class EvaluationListComponent implements OnInit {

  filterValue: string = '';
  answerSheetList: any[] = [];
  evaluationFilterValue: string = "all";
  markFilterValue: string = "allmarks";
  displayedColumns: string[] = ['courseCode', 'courseName', 'dummyNumber', 'totalObtainedMark', 'actions'];
  dataSource = new MatTableDataSource<any>([]);

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  constructor(
    private toastr: ToastrService,
    private evaluationService: EvaluationService,
    private router: Router

  ) {
    this.dataSource.filterPredicate = (data: any, filter: string) => {
      if(this.evaluationFilterValue == 'all') {
        console.log("helso all")
        switch (this.markFilterValue) {
          case "below40": {
            return data.totalObtainedMark < 40;
          }
          case "40to44": {
            return data.totalObtainedMark >= 40 && data.totalObtainedMark < 45;
          }
          case "above44": {
            return data.totalObtainedMark > 44;
          }
          default: {
            return data;
          }
        }
      }
      else if(this.evaluationFilterValue == 'true') {
        switch (this.markFilterValue) {
          case "below40": {
            return data.isEvaluateCompleted === true
            && data.totalObtainedMark < 40;
          }
          case "40to44": {
            return data.isEvaluateCompleted === true
            && data.totalObtainedMark >= 40 && data.totalObtainedMark < 45;
          }
          case "above44": {
            return data.isEvaluateCompleted === true
            && data.totalObtainedMark > 44;
          }
          default: {
            return data.isEvaluateCompleted === true
            && data;
          }
        }
      }
      else if(this.evaluationFilterValue == 'false') {
        switch (this.markFilterValue) {
          case "below40": {
            return data.isEvaluateCompleted === false
            && data.totalObtainedMark < 40;
          }
          case "40to44": {
            return data.isEvaluateCompleted === false
            && data.totalObtainedMark >= 40 && data.totalObtainedMark < 45;
          }
          case "above44": {
            return data.isEvaluateCompleted === false
            && data.totalObtainedMark > 44;
          }
          default: {
            return data.isEvaluateCompleted === false
            && data;
          }
        }
      }
      
    };
  }

  ngOnInit(): void {
    console.log("ngOnInit.................");
    const loggedInUser = localStorage.getItem('userData') || "";
    if (loggedInUser) {
      //console.log("userData: ", JSON.parse(loggedInUser));
      const userData = JSON.parse(loggedInUser);
      this.loadPrimaryDetails(userData.userId);
    }
    this.evaluationFilterValue = "all"
  }

  loadPrimaryDetails(userId: number) {
    this.evaluationService.getAnswerSheetDetails(userId, 0).subscribe(
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

  evaluationFilter(event: any) {
    this.evaluationFilterValue = event.value
    this.dataSource.filter = event.value.trim().toLowerCase();
  }

  markFilter(event: any) {
    this.markFilterValue = event.value
    this.dataSource.filter = event.value.trim().toLowerCase();
  }

  evaluate(answerSheetId: number) {
    //let primaryData = this.answerSheetList.filter(x => x.answersheetId == answerSheetId)[0];
    this.router.navigate(['/apps/evaluate', encode(String(answerSheetId))]);
  }

}

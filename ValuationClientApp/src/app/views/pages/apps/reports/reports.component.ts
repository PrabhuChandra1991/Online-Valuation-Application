import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { ReportService } from '../../services/report.service';
import * as XLSX from 'xlsx';
import * as FileSaver from 'file-saver';

@Component({
  selector: 'app-reports',
  imports: [
    CommonModule,
  ],
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss'
})
export class ReportsComponent {
  dataSourceExamTypes: string[] = [];
  selectedExamType: string;
  toasterService: any;
  spinnerService: any;

  constructor(private reportService: ReportService) { }

  ngOnInit(): void {
    this.loadAllExamTypes();
  }

  loadAllExamTypes() {
    this.dataSourceExamTypes = ['Consolidated Report', 'Pass Analysis Report', 'Fail Analysis Report'];
  }

  onExamTypeChange(event: Event) {
    this.selectedExamType = (event.target as HTMLSelectElement).value;
  }

  fileName: string;
  contentType: string = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';

  export() {
    if (this.selectedExamType == 'Consolidated Report') {
      this.fileName = 'ConsolidateReport';
      this.reportService.GetConsolidatedMarkReport().subscribe({
        next: (response: any) => {
          const headers = ['COLLEGE_CODE', 'COURSE_CODE', 'COURSE_NAME', 'EXAM_TYPE', 'REGISTERED_STUDENT_COUNT',
            'APPEARED_ANSWER_SHEET_COUNT', 'ABSENT_COUNT', 'PASS_COUNT', 'FAIL_COUNT', 'EVALUATION_NOT_COMPLETED_COUNT'];

          const dataRows = response.map((item: any) => [
            item.institutionCode,
            item.courseCode,
            item.courseName,
            item.examType,
            item.studentTotalRegisteredCount,
            item.studentTotalAppearedCount,
            item.studentTotalAbsentCount,
            item.studentTotalPassCount,
            item.studentTotalFailCount,
            item.pendingEvaluationCount
          ]);
          const finalData = [headers, ...dataRows];
          this.openFileInTab(finalData, this.fileName, this.contentType);
        }
      });
    }
    else if (this.selectedExamType == 'Pass Analysis Report') {
      this.fileName = 'PassAnalysisReport';
      this.reportService.GetPassAnalysisReport().subscribe({
        next: (response: any) => {
          const headers = ['COLLEGE_CODE', 'COURSE_CODE', 'COURSE_NAME', 'EXAM_TYPE', 'REGISTERED_STUDENT_COUNT', 'APPEARED_ANSWER_SHEET_COUNT',
            'ABSENT_COUNT', 'TOTAL_PASS_COUNT', 'EVALUATION_NOT_COMPLETED_COUNT', '91-100', '81-90', '71-80', '61-70', '51-60', '45-50'];

          const dataRows = response.map((item: any) => [
            item.institutionCode,
            item.courseCode,
            item.courseName,
            item.examType,
            item.studentTotalRegisteredCount,
            item.studentTotalAppearedCount,
            item.studentTotalAbsentCount,
            item.studentTotalPassCount,
            item.pendingEvaluationCount,
            item.studentTotal_91_100_Count,
            item.studentTotal_81_90_Count,
            item.studentTotal_71_80_Count,
            item.studentTotal_61_70_Count,
            item.studentTotal_51_60_Count,
            item.studentTotal_45_50_Count
          ]);
          const finalData = [headers, ...dataRows];
          this.openFileInTab(finalData, this.fileName, this.contentType);
        },
      });
    }
    else if (this.selectedExamType == 'Fail Analysis Report') {
      this.fileName = 'FailAnalysisReport';
      this.reportService.GetFailAnalysisReport().subscribe({
        next: (response: any) => {
          const headers = ['COLLEGE_CODE', 'COURSE_CODE', 'COURSE_NAME', 'EXAM_TYPE', 'REGISTERED_STUDENT_COUNT', 'APPEARED_ANSWER_SHEET_COUNT',
            'ABSENT_COUNT', 'TOTAL_FAIL_COUNT', 'EVALUATION_NOT_COMPLETED_COUNT', '40-44', '35-39', '30-34', '25-29', '0-24'];

          const dataRows = response.map((item: any) => [
            item.institutionCode,
            item.courseCode,
            item.courseName,
            item.examType,
            item.studentTotalRegisteredCount,
            item.studentTotalAppearedCount,
            item.studentTotalAbsentCount,
            item.studentTotalFailCount,
            item.pendingEvaluationCount,
            item.studentTotal_40_44_Count,
            item.studentTotal_35_39_Count,
            item.studentTotal_30_34_Count,
            item.studentTotal_25_29_Count,
            item.studentTotal_00_24_Count
          ]);
          const finalData = [headers, ...dataRows];
          this.openFileInTab(finalData, this.fileName, this.contentType);
        },
      });
    }
  }

  openFileInTab(data: any[], fileName: string, contentType: string) {
    const worksheet = XLSX.utils.aoa_to_sheet(data);
    const workbook = { Sheets: { Sheet1: worksheet }, SheetNames: ['Sheet1'] };
    const excelBuffer: any = XLSX.write(workbook, { bookType: 'xlsx', type: 'array' });

    const blob = new Blob([excelBuffer], {
      type: contentType
    });

    FileSaver.saveAs(blob, `${fileName}.xlsx`);
  }

}

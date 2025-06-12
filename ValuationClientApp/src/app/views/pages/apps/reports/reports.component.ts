import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { ReportService } from '../../services/report.service';

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
selectedExamType:string;
  toasterService: any;
  spinnerService: any;

 constructor(private reportService:ReportService) { }

ngOnInit(): void {
    this.loadAllExamTypes();
  }
  loadAllExamTypes()
  {
     this.dataSourceExamTypes = ['Consolidated Report', 'Pass Analysis Report', 'Fail Analysis Report'];
  }

  onExamTypeChange(event: Event){
    this.selectedExamType = (event.target as HTMLSelectElement).value;
  }
  fileName:string;
  contentType:string = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';
  export(){
     if(this.selectedExamType == 'Consolidated Report')
     {
      this.fileName = 'ConsolidateReport';
       this.reportService.GetConsolidatedMarkReport().subscribe({
        next: (response: any) => {
          this.openFileInTab(response.base64Content, this.fileName , this.contentType);

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
     else if(this.selectedExamType == 'Pass Analysis Report')
     {

     }
     else if(this.selectedExamType == 'Fail Analysis Report')
     {

     }
  }

  // exportMarks(): void {
  //   this.spinnerService.toggleSpinnerState(true);
  //   this.answersheetService.exportMarks(
  //     this.selectedInstituteId, this.selectedCourseId,
  //     this.selectedExamYear, this.selectedExamMonth, this.selectedExamType).subscribe({
  //       next: (response: any) => {
  //         this.openFileInTab(response.base64Content, response.fileName, response.contentType);

  //         this.toasterService.success('Data submitted successfully!');
  //         this.spinnerService.toggleSpinnerState(false);
  //       },
  //       error: (err: any) => {
  //         if (err.error.message === "EVALUATION-NOT-COMPLETED")
  //           this.toasterService.error('Evaluation is not completed for all answersheets. Please check.');
  //         else
  //           this.toasterService.error('Error exporting marks. Please try again.');
  //         this.spinnerService.toggleSpinnerState(false);
  //       },
  //     });
  // }
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

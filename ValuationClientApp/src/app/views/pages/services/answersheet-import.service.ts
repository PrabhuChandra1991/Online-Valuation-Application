import { Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs/internal/Observable';
import { AnswersheetAllocateInputModel } from '../models/answersheetMark.model';

@Injectable({
  providedIn: 'root',
})
export class AnswersheetImportService {
  private apiUrl = environment.apiURL; // Update this with your actual API URL

  private httpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json',
    }),
  };

  constructor(private http: HttpClient) { }

  public GetExaminationCourseItems(
    institutionId: number,
    examYear: string,
    examMonth: string
  ): Observable<any> {
    return this.http.get(
      `${this.apiUrl}/api/AnswersheetImport/GetExaminationCourseInfo?institutionId=${institutionId}&examYear=${examYear}&examMonth=${examMonth}`
    );
  }

  importAnswerSheetDummyNumbers(formData: FormData, institutionId: number, examYear: string, examMonth: string, courseId: number): Observable<any> {
    const url: string = `${this.apiUrl}/api/AnswersheetImport/ImportDummyNoFromExcelByCourse`;
    const httpOptions = {
      headers: new HttpHeaders({
        institutionId: institutionId,
        examYear: examYear,
        examMonth: examMonth,
        courseId: courseId
      })
    };
    return this.http.post(url, formData, httpOptions);
  }

  public GetAnswersheetImports(institutionId: number, examYear: string, examMonth: string, courseId: number): Observable<any> {
    const ulr = `${this.apiUrl}/api/AnswersheetImport/GetAnswersheetImports`;
    const httpOptions = {
      headers: new HttpHeaders({
        institutionId: institutionId,
        examYear: examYear,
        examMonth: examMonth,
        courseId: courseId
      })
    };
    return this.http.get(ulr, httpOptions);
  }

  public GetAnswersheetImportDetails(
    answersheetImportId: number
  ): Observable<any> {
    const ulr = `${this.apiUrl}/api/AnswersheetImport/GetAnswersheetImportDetails?answersheetImportId=${answersheetImportId}`;
    return this.http.get(ulr);
  }


  ReviewCompletedAndApproving(answersheetImportId: number): Observable<any> {
    const loggedData = localStorage.getItem('userData');
    let userData: any;
    if (loggedData) {
      userData = JSON.parse(loggedData);
    }
    const url: string = `${this.apiUrl}/api/AnswersheetImport/ReviewedAndApproveDummyNumbers?answersheetImportId=${answersheetImportId}`;
    const httpOptions = {
      headers: new HttpHeaders({
        loggedInUserId: parseInt(userData.userId),
      }),
    };
    return this.http.get(url, httpOptions);
  }

  DeleteAnswersheetImportedData(answersheetImportId: number): Observable<any> {
    const loggedData = localStorage.getItem('userData');
    let userData: any;
    if (loggedData) {
      userData = JSON.parse(loggedData);
    }
    const url: string = `${this.apiUrl}/api/AnswersheetImport/DeleteAnswersheetImport?answersheetImportId=${answersheetImportId}`;
    const httpOptions = {
      headers: new HttpHeaders({
        loggedInUserId: parseInt(userData.userId),
      }),
    };
    return this.http.get(url, httpOptions);
  }

  //
  //
  //
}

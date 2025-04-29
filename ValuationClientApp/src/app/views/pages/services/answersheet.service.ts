import { Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs/internal/Observable';
import { AnswersheetAllocateInputModel } from '../models/answersheetMark.model';

@Injectable({
  providedIn: 'root',
})
export class AnswersheetService {
  private apiUrl = environment.apiURL; // Update this with your actual API URL

  private httpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json',
    }),
  };

  constructor(private http: HttpClient) {}

  getAnswersheetDetails(
    institutionId: number,
    courseId: number
  ): Observable<any> {
    return this.http.get(
      `${this.apiUrl}/api/Answersheet/GetAnswersheetDetails?institutionId=${institutionId}&courseId=${courseId}`
    );
  }

  getCourses(): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/Course`);
  }

  getExamMonths(): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/Dropdown/GetExamMonths`);
  }

  getExamYears(): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/Dropdown/GetExamYears`);
  }

  getConsolidatedExamAnswersheets(institutionId: number): Observable<any> {
    return this.http.get(
      `${this.apiUrl}/api/Answersheet/GetConsolidatedExamAnswersheets?institutionId=${institutionId}`
    );
  }

  AllocateAnswerSheetsToUser(
    examinationId: number,
    userId: number,
    noofsheets: number
  ): Observable<any> {
    let url = `${this.apiUrl}/api/Answersheet/AllocateAnswerSheetsToUser`;

    var headers = new HttpHeaders({ 'Content-Type': 'application/json' });

    var inputData: AnswersheetAllocateInputModel = {
      examinationId: examinationId,
      userId: userId,
      noofsheets: noofsheets,
    };

    return this.http.post(url, JSON.stringify(inputData), { headers });
  }
  exportMarks(institutionId: number, courseId: number,examYear:string,examMonth:string): Observable<any> {
    return this.http.get(
      `${this.apiUrl}/api/Answersheet/ExportMarks?institutionId=${institutionId}&courseId=${courseId}&examYear=${examYear}&examMonth=${examMonth}`
);
  }

}

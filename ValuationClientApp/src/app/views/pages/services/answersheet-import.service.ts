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

  constructor(private http: HttpClient) {}

  public GetExaminationItems(
    institutionId: number,
    examYear: string,
    examMonth: string
  ): Observable<any> {
    return this.http.get(
      `${this.apiUrl}/api/AnswersheetImport/GetExaminationInfo?institutionId=${institutionId}&examYear=${examYear}&examMonth=${examMonth}`
    );
  }

  getConsolidatedExamAnswersheets(institutionId: number): Observable<any> {
    return this.http.get(
      `${this.apiUrl}/api/Answersheet/GetConsolidatedExamAnswersheets?institutionId=${institutionId}`
    );
  }
}

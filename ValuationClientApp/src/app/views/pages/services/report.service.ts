import { Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs/internal/Observable';
import { AnswersheetAllocateInputModel } from '../models/answersheetMark.model';

@Injectable({
  providedIn: 'root',
})
export class ReportService {
  private apiUrl = environment.apiURL; // Update this with your actual API URL

  private httpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json',
    }),
  };

  constructor(private http: HttpClient) { }

  GetConsolidatedMarkReport(): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/Report/GetConsolidatedMarkReport`);
  }

  GetPassAnalysisReport(): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/Report/GetPassAnalysisReport`);
  }

  GetFailAnalysisReport(): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/Report/GetFailAnalysisReport`);
  }

}

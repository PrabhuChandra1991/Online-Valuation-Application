import { Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs/internal/Observable';

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

  getAnswersheetDetails(institutionId: number, courseId: number): Observable<any> {
    return this.http.get(
      `${this.apiUrl}/api/Answersheet/GetAnswersheetDetails?institutionId=${institutionId}&courseId=${courseId}`
    );
  }

  getCourses(): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/Course`);
  }

getConsolidatedExamAnswersheets(institutionId: number): Observable<any> {
    return this.http.get(
      `${this.apiUrl}/api/Answersheet/GetConsolidatedExamAnswersheets?institutionId=${institutionId}`
    );
  }


}

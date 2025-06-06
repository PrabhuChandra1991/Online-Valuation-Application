import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { AnswersheetMark } from '../models/answersheetMark.model';

@Injectable({
  providedIn: 'root'
})

export class EvaluationService {

  private apiUrl = environment.apiURL; // Update this with your actual API URL

  private httpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json'
    })
  };

  constructor(private http: HttpClient) { }

  getAnswerSheetDetails(userId: number, answersheetId: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/Answersheet/GetAnswersheetDetails?allocatedToUserId=${(userId != 0) ? userId : ''}&answersheetId=${(answersheetId != 0) ? answersheetId : ''}`);
  }

  getQuestionAndAnswer(answersheetId: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/Answersheet/GetQuestionAndAnswersByAnswersheetId?answersheetId=${answersheetId}`);
  }

  getAnswersheetMark(answersheetId: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/Answersheet/GetAnswersheetMark?answersheetId=${answersheetId}`);
  }

  getAnswersheetPdfAvailable(answersheetId: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/Answersheet/GetAnswerSheetAvailable?answersheetId=${answersheetId}`);
  }

  saveAnswersheetMark(answersheetMark: AnswersheetMark): Observable<any> {
    let url = `${this.apiUrl}/api/Answersheet/SaveAnswersheetMark/`;
    return this.http.post(url, JSON.stringify(answersheetMark), this.httpOptions);
  }

  completeEvaluation(answersheetId: number, evaluatedByUserId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/Answersheet/CompleteEvaluation?answersheetId=${answersheetId}&evaluatedByUserId=${evaluatedByUserId}`, {}, this.httpOptions);
  }

  getAnswersheetDetailsById(answersheetId: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/Answersheet/GetAnswersheetDetailsById?answersheetId=${answersheetId}`);
  }

}
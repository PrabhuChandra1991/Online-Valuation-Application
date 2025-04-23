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

  getAnswerSheetDetails(userId: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/Answersheet/GetAnswersheetDetails?allocatedToUserId=${userId}`);
  }

  getQuestionAndAnswer(answersheetId: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/Answersheet/GetQuestionAndAnswersByAnswersheetId?answersheetId=${answersheetId}`);
  }

  getAnswersheetMark(answersheetId: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/Answersheet/GetAnswersheetMark?answersheetId=${answersheetId}`);
  }

  saveAnswersheetMark(answersheetMark: AnswersheetMark): Observable<any> {
    let url = `${this.apiUrl}/api/Answersheet/SaveAnswersheetMark/`;
    return this.http.post(url, JSON.stringify(answersheetMark), this.httpOptions);
  }

  completeEvaluation(answersheetId: number, evaluatedByUserId: number): Observable<any> {    
    return this.http.post(`${this.apiUrl}/api/Answersheet/CompleteEvaluation?answersheetId=${answersheetId}&evaluatedByUserId=${evaluatedByUserId}`, {}, this.httpOptions);
  }

}
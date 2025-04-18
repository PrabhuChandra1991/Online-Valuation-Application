import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { options } from '@fullcalendar/core/preact.js';

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

    // getAllQPAK(qpTemplateBody: any): Observable<any> {
    //   let url = `${this.apiUrl}/api/QpTemplate/GetAllQPAKDetails/`;
    //   var headers = new HttpHeaders({'Content-Type': 'application/json'});
    //   return  this.http.post(url, JSON.stringify(qpTemplateBody), { headers });
    // }

}
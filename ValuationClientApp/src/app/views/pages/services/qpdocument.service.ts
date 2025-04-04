import { Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs/internal/Observable';


@Injectable({
  providedIn: 'root'
})
export class QPDocumentService {

   private apiUrl = environment.apiURL; // Update this with your actual API URL

      private httpOptions = {
          headers: new HttpHeaders({
            'Content-Type': 'application/json'
          })
        };

        constructor(private http: HttpClient) { }

      downloadQPFile(documentId: number): Observable<any> {
          return this.http.get(`${this.apiUrl}/api/BlobStorage/download/${documentId}`);
        }
      validateQPFile(formData: any, documentId: number): Observable<any> {
        let resp = this.http.post(`${this.apiUrl}/api/QpTemplate/ValidateGeneratedQP/${documentId}`, formData);
        return resp;
        }

  submitGeneratedQP(formData: any, documentId: number, isGraphsRequired: boolean, graphName: string, isTablesAllowed: boolean, tableName: string): Observable<any> {
    let resp = this.http.post(`${this.apiUrl}/api/QpTemplate/SubmitGeneratedQP/${documentId}?IsGraphsRequired=${isGraphsRequired}&GraphName=${graphName}&isTablesAllowed=${isTablesAllowed}&tableName=${tableName}`, formData);
    return resp;
  }

}

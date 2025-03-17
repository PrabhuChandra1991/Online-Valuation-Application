import { Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs/internal/Observable';


@Injectable({
  providedIn: 'root'
})
export class ImportService {

   private apiUrl = environment.apiURL; // Update this with your actual API URL
  
      private httpOptions = {
          headers: new HttpHeaders({
            'Content-Type': 'application/json'
          })
        };
  
        constructor(private http: HttpClient) { }
      
      importData(formData: FormData): Observable<any> {
          return this.http.post(`${this.apiUrl}/api/QPDataImport/importQPDataByExcel`, formData);
        }
  
}

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { FormGroup, FormControl, Validators } from '@angular/forms';

import { HttpClientModule, HttpClient } from '@angular/common/http';
import { ToastrService } from 'ngx-toastr';
import { SpinnerService } from '../../services/spinner.service';
import { ImportService } from '../../services/import.service';

@Component({
  selector: 'app-master',
  imports: [CommonModule, FormsModule, ReactiveFormsModule, HttpClientModule],
  templateUrl: './master.component.html',
  styleUrl: './master.component.scss'
})
export class MasterComponent {

  isSubmitting = false;

  isCourseSylImported=false;
  isMasImported=false;
  isQpAkImported=false;

  fileName = '';
  syllabusDocsPending='';
  duplicateImportAlert = '';
  masterDataForm = new FormGroup({
    name: new FormControl('', [Validators.required, Validators.minLength(3)]),
    file: new FormControl('', [Validators.required]),
    fileSource: new FormControl('', [Validators.required])
  });

  courseSyllabusDocumentsForm = new FormGroup({
    name: new FormControl('', [Validators.required, Validators.minLength(3)]),
    file: new FormControl([], [Validators.required]),
    fileSource: new FormControl([], [Validators.required])
  });
  qpDocumentsForm = new FormGroup({
    name: new FormControl('', [Validators.required, Validators.minLength(3)]),
    file: new FormControl([], [Validators.required]),
    fileSource: new FormControl([], [Validators.required])
  });
  constructor(private toastr: ToastrService,private ImportService: ImportService,private spinnerService : SpinnerService) {
  }

  get f(){
    return this.masterDataForm.controls;
  }

  onMasterDataFileChange(event:any) {
    if (event.target.files.length > 0) {
      this.isMasImported = true;
      const file = event.target.files[0];
      this.masterDataForm.patchValue({
        fileSource: file
      });
    }
  }

  submitMasterQPData(){

    this.spinnerService.toggleSpinnerState(true);
    const formData = new FormData();

    const fileSourceValue = this.masterDataForm.get('fileSource')?.value;

    if (fileSourceValue !== null && fileSourceValue !== undefined) {
        formData.append('file', fileSourceValue);
    }

    this.ImportService.importData(formData)
    .subscribe({
      next: (response) => {
        this.duplicateImportAlert = response.message;
        this.toastr.success(response.message);
        this.f['file'].setValue('');
      },
      error: () => {
        this.toastr.error('Failed to import data. Please try again.');
        this.spinnerService.toggleSpinnerState(false);
      },
      complete: () => {
        this.spinnerService.toggleSpinnerState(false);
       }
    });

  }

  get courseSyllabusDocumentsFormF(){
    return this.courseSyllabusDocumentsForm.controls;
  }
  onCourseSyllabusDocumentFileChange(event:any) {
    if (event.target.files.length > 0) {
      this.isCourseSylImported = true;
      this.courseSyllabusDocumentsForm.patchValue({
        fileSource: event.target.files
      });
    }
  }

  submitSyllabusDocuments(){

    this.spinnerService.toggleSpinnerState(true);
    const formData = new FormData();
    this.syllabusDocsPending ='';
    const fileSourceValue = this.courseSyllabusDocumentsForm.get('fileSource')?.value;

    if (fileSourceValue !== null && fileSourceValue !== undefined) {
      for(let i=0;i<fileSourceValue.length;i++){
        formData.append('files', fileSourceValue[i]);
      }

    }

    this.ImportService.importSyllabusDocuments(formData)
    .subscribe({
      next: (response) => {
        this.syllabusDocsPending = response.message;
        this.toastr.success('Data imported successfully!');
        this.courseSyllabusDocumentsFormF['file'].setValue([]);
      },
      error: () => {
        this.toastr.error('Failed to import data. Please try again.');
        this.spinnerService.toggleSpinnerState(false);
      },
      complete: () => {
        this.spinnerService.toggleSpinnerState(false);
       }
    });

  }

  get qpDocumentsformf(){
    return this.qpDocumentsForm.controls;
  }
  onQpDocumentFileChange(event:any) {
    if (event.target.files.length > 0) {
      this.isQpAkImported = true;
      this.qpDocumentsForm.patchValue({
        fileSource: event.target.files
      });
    }
  }
  submitQPDocuments(){

    this.spinnerService.toggleSpinnerState(true);
    const formData = new FormData();

    const fileSourceValue = this.qpDocumentsForm.get('fileSource')?.value;

    if (fileSourceValue !== null && fileSourceValue !== undefined) {
      for (let i = 0; i < fileSourceValue.length; i++) {
        formData.append('files', fileSourceValue[i]);
      }
    }

    this.ImportService.importQPDocuments(formData)
    .subscribe({
      next: () => {
        this.toastr.success('Data imported successfully!');
        this.qpDocumentsformf['file'].setValue([]);
      },
      error: () => {
        this.toastr.error('Failed to import data. Please try again.');
        this.spinnerService.toggleSpinnerState(false);
      },
      complete: () => {
        this.spinnerService.toggleSpinnerState(false);
       }
    });

  }
}

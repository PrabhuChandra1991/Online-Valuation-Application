import { Component, OnInit } from '@angular/core';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { NgxExtendedPdfViewerModule } from 'ngx-extended-pdf-viewer';
import { ContentChange, QuillModule, SelectionChange } from 'ngx-quill'

@Component({
  selector: 'app-answer-valuation',
  imports: [
    RouterLink,
    FormsModule,
    NgxExtendedPdfViewerModule,
    QuillModule,
],
  templateUrl: './answer-valuation.component.html',
  styleUrl: './answer-valuation.component.scss'
})
export class AnswerValuationComponent implements OnInit{
  htmlText = `<p> If You Can Think It, You Can Do It. </p>`
  quillConfig = {
     toolbar: {
       container: [
         ['bold', 'italic', 'underline', 'strike'],        // toggled buttons
         ['code-block'],
        //  [{ 'header': 1 }, { 'header': 2 }],               // custom button values
         [{ 'list': 'ordered'}, { 'list': 'bullet' }],
         [{ 'script': 'sub'}, { 'script': 'super' }],      // superscript/subscript
         [{ 'indent': '-1'}, { 'indent': '+1' }],          // outdent/indent
        //  [{ 'direction': 'rtl' }],                         // text direction

        //  [{ 'size': ['small', false, 'large', 'huge'] }],  // custom dropdown
         [{ 'header': [1, 2, 3, 4, 5, 6, false] }],

         [{ 'align': [] }],

        //  ['clean'],                                         // remove formatting button

        //  ['link'],
        //  ['link', 'image', 'video']
       ],
     },
  }

  currentPage = 1;
  totalPages = 10; // Change this based on your PDF

  nextPage() {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
    }
  }

  prevPage() {
    if (this.currentPage > 1) {
      this.currentPage--;
    }
  }
  onSelectionChanged = (event: SelectionChange) => {
    if(event.oldRange == null) {
      this.onFocus();
    }
    if(event.range == null) {
      this.onBlur();
    }
  }

  onContentChanged = (event: ContentChange) => {
    // console.log(event.html);
  }

  onFocus = () => {
    console.log("On Focus");
  }
  onBlur = () => {
    console.log("Blurred");
  }


  showImageCropper: boolean = false;
  imageUrl: string = 'images/others/placeholder.jpg';
  // imageUrl: string = '';
  isLoadImageFailed: boolean = false;
  isNoFileChosen: boolean = false;

  imageChangedEvent: Event | null = null;
  croppedImage: SafeUrl  = '';

  constructor(private sanitizer: DomSanitizer) {}

  ngOnInit(): void {
    if (this.imageUrl) {
      this.showImageCropper = true;
    }
  }

  fileChangeEvent(event: Event): void {
    this.showImageCropper = false;
    const target = event.target as HTMLInputElement | null;
    if (target?.files?.length) {
      this.imageChangedEvent = event;
      this.showImageCropper = true;
      this.isLoadImageFailed = false;
      this.isNoFileChosen = false;
      this.imageUrl = '';
    } else {
      this.isNoFileChosen = true;
      this.isLoadImageFailed = false;
    }

  }



  cropperReady() {
    // cropper ready
  }

  loadImageFailed() {
    // show message
    this.showImageCropper = false;
    this.isLoadImageFailed = true;
    this.isNoFileChosen = false;
    console.log('Failed to load image');
  }

}

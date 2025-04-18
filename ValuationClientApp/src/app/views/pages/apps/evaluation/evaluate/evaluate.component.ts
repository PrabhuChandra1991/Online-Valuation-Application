import { NgClass, NgFor } from '@angular/common';
import { OnInit, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PdfViewerModule } from 'ng2-pdf-viewer';
import { Question } from '../../../models/question.model';
import { ToastrService } from 'ngx-toastr';
import { EvaluationService } from '../../../services/evaluation.service';
import { encode, decode } from 'js-base64';
import { ActivatedRoute } from '@angular/router'

@Component({
  selector: 'app-evaluate',
  imports: [NgClass, NgFor, CommonModule, PdfViewerModule],
  templateUrl: './evaluate.component.html',
  styleUrl: './evaluate.component.scss'
})

export class EvaluateComponent implements OnInit {
  isValue: any;
  hasQuestion: boolean = false;
  primaryData: any;
  qaList: any[] = [];
  part: any;
  partList: any[] = [];
  totalMarks: number = 0;
  obtainedMarks: number = 0;
  activeQuestion: string = '---------- Please select the question above to load here ----------';
  activeQuestionImg: string = '';
  activeAnswer: string = '---------- Answerkey loads here ----------';
  activeQuestionMark: string = '';
  src = "https://vadimdez.github.io/ng2-pdf-viewer/assets/pdf-test.pdf";
  //src = 'https://iris.who.int/bitstream/handle/10665/137592/roadmapsitrep_7Nov2014_eng.pdf';

  constructor(
    private route: ActivatedRoute,
    private toastr: ToastrService,
    private evaluationService: EvaluationService
  ) { }

  ngOnInit(): void {
    console.log("ngOnInit.................");
    this.route.paramMap.subscribe(params => {
      let encodedPrimaryData = params.get('data') || "";
      this.primaryData = JSON.parse(decode(encodedPrimaryData));
      console.log(this.primaryData);
      if (this.primaryData.answersheetId) {
        this.loadQuestionPaperAnswerKey(this.primaryData.answersheetId);
      }
    });
  }

  loadQuestionPaperAnswerKey(answerSheetId: number) {
    console.log('loading questions............');
    this.evaluationService.getQuestionAndAnswer(answerSheetId).subscribe(
      (data: any[]) => {
        if (data.length > 0) {
          let quest = {};
          data.forEach((item) => {
            let mark = 0;
            const dom = new DOMParser().parseFromString(decode(item.questionMark), 'text/html');
            let spans = dom.querySelectorAll('span');
            spans.forEach((span) => {
              let innerHTML = span.innerHTML.replace(/&nbsp;/g, '').trim();
              if ((innerHTML.length > 0)) {
                mark = parseInt(innerHTML);
              }
            });
            this.totalMarks = this.totalMarks + mark;
            quest = { "questionId": item.questionNumber, "questionPart": item.questionPartName, "questionNumber": item.questionNumberDisplay, "questionDescription": decode(item.questionDescription), "questionImage": decode(item.questionImage), "answerDescription": decode(item.answerDescription), "mark": mark, "obtainedMark": 0, "hasOption": "" }
            this.qaList.push(quest);
            this.hasQuestion = true;
          });
          console.log("qaList: ", this.qaList);
          this.getPartList();
        }
      },
      (error) => {
        console.error('Error fetching question and answer:', error);
        this.toastr.error('Failed to load question and answer.');
      }
    );
  }

  getPartList() {
    console.log("getPartList...............");
    const partMap = new Map();
    this.qaList.forEach(({ questionPart }) => {
      partMap.set(questionPart, (partMap.get(questionPart) || 0) + 1);
    });
    this.partList = Array.from(partMap, ([part, count]) => ({ part, count }));
    console.log("partList:", this.partList);

    // let parts = [...new Set(this.qaList.map(item => item.questionPart))];
    // console.log("parts", parts);
    // parts.forEach(item => {
    //   let count = this.qaList.filter(e => e.questionPart === item).length;
    //   this.part = { "part": item, "count": count }; 
    //   this.partList.push(this.part);
    // });
    // console.log("partList: " + this.partList);
  }

  getQuestionDetails(event: any) {
    console.log("getting question.................");

    let btnList = document.querySelectorAll(".question .btn");
    let txtList = document.querySelectorAll(".question .form-control");
    let txtid = `#${event.target.id.replace('btn', 'txt')}`;
    let txt = document.querySelector(txtid);

    btnList.forEach((item) => {
      if (item.classList.contains('active')) {
        item.classList.remove('active');
      }
    });
    txtList.forEach((item) => {
      if (item.classList.contains('active')) {
        item.classList.remove('active');
      }
    });

    event.srcElement.classList.add('active');
    txt?.classList.add('active');

    this.isValue = event.target.innerText;
    //console.log(this.isValue);
    let matchedItem = this.qaList.filter(x => x.questionNumber == this.isValue)[0];
    //console.log(matchedItem);
    this.activeQuestion = matchedItem.questionDescription;
    this.activeQuestionImg = matchedItem.questionImage;
    this.activeAnswer = matchedItem.answerDescription;
    this.activeQuestionMark = matchedItem.mark;
  }

  highlightQuestion(event: any) {
    // let btnList = document.querySelectorAll(".question .btn");
    // let txtList = document.querySelectorAll(".question .form-control");
    // let btnId = `#${event.target.id.replace('txt','btn')}`;
    // let btn = document.querySelector(btnId)

    // btnList.forEach((item) => {
    //   if (item.classList.contains('active')) {
    //     item.classList.remove('active');
    //   }
    // });
    // txtList.forEach((item) => {
    //   if (item.classList.contains('active')) {
    //     item.classList.remove('active');
    //   }
    // });

    // event.srcElement.classList.add('active');
    // btn?.classList.add('active');
  }

  validateMark(event: any) {
    if (event.target.value) {
      if (event.target.value.match(/[^0-9]/g)) {
        this.toastr.error('Please add only numbers.');
        event.target.value = event.target.value.replace(/[^\d]/g, '');
        return;
      }
      //console.log(event.target.value + " > " + event.srcElement.max + " = " + (parseFloat(event.target.value) > parseInt(event.srcElement.max)));
      //console.log(event.target.value + " < " + event.srcElement.max + " = " + (parseFloat(event.target.value) < parseInt(event.srcElement.max)));
      //console.log((parseFloat(event.target.value) < parseInt(event.srcElement.max)));
      //set textbox color red if alloted mark greater than the question mark
      if ((event.target.value < 0) || (parseFloat(event.target.value) > parseInt(event.srcElement.max))) {
        event.srcElement.classList.add('error');
      }
      else {
        event.srcElement.classList.remove('error');
      }
      this.obtainedMarks = 0;
      let txtList = document.querySelectorAll(".question .form-control") as NodeListOf<HTMLInputElement>;
      txtList.forEach((item: HTMLInputElement) => {
        if (item.value) {
          this.obtainedMarks += parseFloat(item.value); // sum all text box marks
        }
        else {
          item.classList.remove('error'); // remove red color if the mark is removed or empty
        }
      });
    }
  }

  filter(part: string) {
    let list = this.qaList.filter(e => e.part === part);
    return list;
  }

  onError(error: any) {
    console.error('PDF error: ', error);
  }

  // getQuestionData() {
  //   for (let index = 1; index <= 30; index++) {  
  //     let part = "A";
  //     if (index>10 && index<=20) {
  //       part = "B"
  //     }
  //     else if (index>20) {
  //       part = "C"
  //     }
  //     let isOpt = false;
  //     if ((index == 11) || (index == 13) || (index == 15) || (index == 17) || (index == 19)) {
  //       isOpt = true;
  //     }
  //     if ((index == 21) || (index == 23) || (index == 25) || (index == 27) || (index == 29)) {
  //       isOpt = true;
  //     }
  //     console.log(index + "=" + isOpt);
  //     let quest = { "questionId": index, "part": part, "questionNumber": index, "question": "Question "+ index , "answerKey": "This is the answer for question " + index, "mark": index, "allotedMark": 0, "isOptional": isOpt }
  //     this.qaList.push(quest);
  //   }    
  // }

}

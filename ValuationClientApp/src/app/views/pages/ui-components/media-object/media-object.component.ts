import { Component, OnInit } from '@angular/core';
import { CodePreviewComponent } from '../../../partials/code-preview/code-preview.component';

const defaultMediaObject = {
  htmlCode: 
`<div class="d-flex align-items-start">
	<img src="..." class="w-100px w-sm-200px me-3" alt="...">
	<div>
		<h5 class="mb-2">Media heading</h5>
		<p>Cras sit amet nibh libero, in gravida nulla. Nulla vel metus scelerisque ante sollicitudin. Cras purus odio, vestibulum in vulputate at, tempus viverra turpis. Fusce condimentum nunc ac nisi vulputate fringilla. Donec lacinia congue felis in faucibus.</p>
	</div>
</div>`,
  tsCode: 
`import { Component } from '@angular/core';

@Component({
  selector: 'app-media-object',
  standalone: true,
  imports: [],
  templateUrl: './media-object.component.html'
})
export class MediaObjectComponent {}`
}

const mediaObjectNesting = {
  htmlCode: 
`<div class="d-flex align-items-start">
	<img src="..." class="w-100px w-sm-150px me-3" alt="...">
	<div>
		<h5 class="mb-2">Media heading</h5>
		Cras sit amet nibh libero, in gravida nulla. Nulla vel metus scelerisque ante sollicitudin. Cras purus odio, vestibulum in vulputate at, tempus viverra turpis. Fusce condimentum nunc ac nisi vulputate fringilla. Donec lacinia congue felis in faucibus.

		<div class="d-flex align-items-start mt-3">
			<a class="me-3" href="#">
				<img src="..." class="w-100px w-sm-150px" alt="...">
			</a>
			<div>
				<h5 class="mb-2">Media heading</h5>
				Cras sit amet nibh libero, in gravida nulla. Nulla vel metus scelerisque ante sollicitudin. Cras purus odio, vestibulum in vulputate at, tempus viverra turpis. Fusce condimentum nunc ac nisi vulputate fringilla. Donec lacinia congue felis in faucibus.
			</div>
		</div>
	</div>
</div>`,
  tsCode: 
`import { Component } from '@angular/core';

@Component({
  selector: 'app-media-object',
  standalone: true,
  imports: [],
  templateUrl: './media-object.component.html'
})
export class MediaObjectComponent {}`
}

const mediaObjectAlignment = {
  htmlCode: 
`<div class="d-flex align-items-start">
	<img src="..." class="align-self-start w-100px w-sm-150px me-3" alt="...">
	<div>
		<h5 class="mb-2">Top-aligned media</h5>
		<p>Cras sit amet nibh libero, in gravida nulla. Nulla vel metus scelerisque ante sollicitudin. Cras purus odio, vestibulum in vulputate at, tempus viverra turpis. Fusce condimentum nunc ac nisi vulputate fringilla. Donec lacinia congue felis in faucibus.</p>
		<p>Donec sed odio dui. Nullam quis risus eget urna mollis ornare vel eu leo. Cum sociis natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus.</p>
	</div>
</div>`,
  tsCode: 
`import { Component } from '@angular/core';

@Component({
  selector: 'app-media-object',
  standalone: true,
  imports: [],
  templateUrl: './media-object.component.html'
})
export class MediaObjectComponent {}`
}

const mediaObjectCenterAlign = {
  htmlCode: 
`<div class="d-flex align-items-start">
	<img src="..." class="align-self-center w-100px w-sm-150px me-3" alt="...">
	<div>
		...
	</div>
</div>`,
  tsCode: 
`import { Component } from '@angular/core';

@Component({
  selector: 'app-media-object',
  standalone: true,
  imports: [],
  templateUrl: './media-object.component.html'
})
export class MediaObjectComponent {}`
}

const mediaObjectBottomAlign = {
  htmlCode: 
`<div class="d-flex align-items-start">
	<img src="..." class="align-self-end w-100px w-sm-150px me-3" alt="...">
	<div>
		...
	</div>
</div>`,
  tsCode: 
`import { Component } from '@angular/core';

@Component({
  selector: 'app-media-object',
  standalone: true,
  imports: [],
  templateUrl: './media-object.component.html'
})
export class MediaObjectComponent {}`
}

const mediaObjectOrder = {
  htmlCode: 
`<div class="d-flex align-items-start">
	<div>
		<h5 class="mt-0 mb-1">Media object</h5>
		Cras sit amet nibh libero, in gravida nulla. Nulla vel metus scelerisque ante sollicitudin. Cras purus odio, vestibulum in vulputate at, tempus viverra turpis. Fusce condimentum nunc ac nisi vulputate fringilla. Donec lacinia congue felis in faucibus.
	</div>
	<img src="..." class="w-100px w-sm-150px ms-3" alt="...">
</div>`,
  tsCode: 
`import { Component } from '@angular/core';

@Component({
  selector: 'app-media-object',
  standalone: true,
  imports: [],
  templateUrl: './media-object.component.html'
})
export class MediaObjectComponent {}`
}



const mediaObjectList = {
  htmlCode: 
`<ul class="list-unstyled">
	<li class="d-flex align-items-start">
		<img src="..." class="w-100px w-sm-150px me-3" alt="...">
		<div>
			<h5 class="mt-0 mb-1">List-based media object</h5>
			Cras sit amet nibh libero, in gravida nulla. Nulla vel metus scelerisque ante sollicitudin. Cras purus odio, vestibulum in vulputate at, tempus viverra turpis. Fusce condimentum nunc ac nisi vulputate fringilla. Donec lacinia congue felis in faucibus.
		</div>
	</li>
	<li class="d-flex align-items-start my-4">
		...
	</li>
	<li class="d-flex align-items-start">
		...
	</li>
</ul>`,
  tsCode: 
`import { Component } from '@angular/core';

@Component({
  selector: 'app-media-object',
  standalone: true,
  imports: [],
  templateUrl: './media-object.component.html'
})
export class MediaObjectComponent {}`
}

@Component({
    selector: 'app-media-object',
    imports: [CodePreviewComponent],
    templateUrl: './media-object.component.html'
})
export class MediaObjectComponent implements OnInit {

  defaultMediaObjectCode: any;
  mediaObjectNestingCode: any;
  mediaObjectAlignmentCode: any;
  mediaObjectCenterAlignCode: any;
  mediaObjectBottomAlignCode: any;
  mediaObjectOrderCode: any;
  mediaObjectListCode: any;

  constructor() { }

  ngOnInit(): void {
    this.defaultMediaObjectCode = defaultMediaObject;
    this.mediaObjectNestingCode = mediaObjectNesting;
    this.mediaObjectAlignmentCode = mediaObjectAlignment;
    this.mediaObjectCenterAlignCode = mediaObjectCenterAlign;
    this.mediaObjectBottomAlignCode = mediaObjectBottomAlign;
    this.mediaObjectOrderCode = mediaObjectOrder;
    this.mediaObjectListCode = mediaObjectList;
  }

  scrollTo(element: any) {
    element.scrollIntoView({behavior: 'smooth'});
  }

}

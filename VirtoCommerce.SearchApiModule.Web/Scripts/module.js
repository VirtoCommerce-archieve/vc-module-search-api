//Call this to register our module to main application
var moduleName = "virtoCommerce.searchAPIModule";

if (AppDependencies !== undefined) {
    AppDependencies.push(moduleName);
}

angular.module(moduleName, ['virtoCommerce.catalogModule'])
	.run(['platformWebApp.widgetService', function (widgetService) {

	    // register WIDGETS
	    // integration: index in product details
	    widgetService.registerWidget({
	        controller: 'virtoCommerce.searchAPIModule.indexWidgetController',
	        template: 'Modules/$(VirtoCommerce.SearchApi)/Scripts/widgets/integrations/item-index-widget.tpl.html'
	    }, 'itemDetail');

	    // integration: index in catalog details
	    widgetService.registerWidget({
	        controller: 'virtoCommerce.searchAPIModule.catalogIndexWidgetController',
	        size: [2, 1],
	        template: 'Modules/$(VirtoCommerce.SearchApi)/Scripts/widgets/integrations/catalog-index-widget.tpl.html'
	    }, 'catalogDetail');
	}]);
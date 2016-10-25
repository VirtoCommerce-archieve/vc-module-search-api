//Call this to register our module to main application
var moduleName = "virtoCommerce.searchAPIModule";

if (AppDependencies !== undefined) {
    AppDependencies.push(moduleName);
}

angular.module(moduleName, ['virtoCommerce.catalogModule'])
	.run(['platformWebApp.widgetService', 'platformWebApp.pushNotificationTemplateResolver', 'platformWebApp.bladeNavigationService', function (widgetService, pushNotificationTemplateResolver, bladeNavigationService) {

	    // register WIDGETS
	    // integration: index in product details
	    widgetService.registerWidget({
	        controller: 'virtoCommerce.searchAPIModule.indexWidgetController',
	        template: 'Modules/$(VirtoCommerce.SearchApi)/Scripts/widgets/integrations/item-index-widget.tpl.html'
	    }, 'itemDetail');

	    // integration: index in catalog details
	    widgetService.registerWidget({
	        controller: 'virtoCommerce.searchAPIModule.catalogIndexWidgetController',
	        size: [3, 1],
	        template: 'Modules/$(VirtoCommerce.SearchApi)/Scripts/widgets/integrations/catalog-index-widget.tpl.html'
	    }, 'catalogDetail');

	    // register notification template
	    pushNotificationTemplateResolver.register({
	        priority: 900,
	        satisfy: function (notify, place) { return place == 'history' && notify.notifyType == 'SearchPushNotification'; },
	        template: '$(Platform)/Scripts/app/pushNotifications/blade/historyDefault.tpl.html',
	        action: function (notify) {
	            var blade = {
	                id: 'indexProgress',
	                currentEntity: notify,
	                controller: 'virtoCommerce.searchAPIModule.indexProgressController',
	                template: 'Modules/$(VirtoCommerce.SearchApi)/Scripts/blades/index-progress.tpl.html'
	            };
	            bladeNavigationService.showBlade(blade);
	        }
	    });
	}]);
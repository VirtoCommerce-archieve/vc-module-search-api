angular.module('virtoCommerce.searchModuleApi')
.factory('virtoCommerce.searchModuleApi.search', ['$resource', function ($resource) {
    return $resource('', {}, {     
        queryFilterProperties: { url: 'api/search/storefilterproperties/:id', isArray: true },
        saveFilterProperties: { url: 'api/search/storefilterproperties/:id', method: 'PUT' }
    });
}]);

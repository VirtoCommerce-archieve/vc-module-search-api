angular.module('virtoCommerce.searchAPIModule')
.factory('virtoCommerce.searchAPIModule.searchAPIResources', ['$resource', function ($resource) {
    return $resource('api/search/index/:documentType/:documentId', null, {
        getStatistics: { url: 'api/search/index/statistics/:documentType/:documentId' },
        buildIndex: { method: 'PUT', url: 'api/searchAPI/index/rebuild/:documentType/:id' }
    });
}]);
